using System.Text.Json;
using Fcg.Payments.Application.Constants;
using Fcg.Payments.Application.Exceptions;
using Fcg.Payments.Contracts.Audit;
using Fcg.Payments.Contracts.Events;
using Fcg.Payments.Contracts.Paging;
using Fcg.Payments.Contracts.Payments;
using Fcg.Payments.Domain.Entities;
using Fcg.Payments.Domain.Enums;
using Fcg.Payments.Domain.Repositories;
using Fcg.Payments.Application.Observability;
using Microsoft.Extensions.Logging;

namespace Fcg.Payments.Application.Services;

public class PaymentService : IPaymentService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly IPaymentRepository _paymentRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IPaymentGateway _gateway;
    private readonly IEventPublisher _eventPublisher;
    private readonly IGameApiClient _gameApiClient;
    private readonly IUserInfoService _userInfoService;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly IObservabilityContextAccessor _observability;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IAuditLogRepository auditLogRepository,
        IOutboxRepository outboxRepository,
        IPaymentGateway gateway,
        IEventPublisher eventPublisher,
        IGameApiClient gameApiClient,
        IUserInfoService userInfoService,
        IIdempotencyStore idempotencyStore,
        IObservabilityContextAccessor observability,
        ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _auditLogRepository = auditLogRepository;
        _outboxRepository = outboxRepository;
        _gateway = gateway;
        _eventPublisher = eventPublisher;
        _gameApiClient = gameApiClient;
        _userInfoService = userInfoService;
        _idempotencyStore = idempotencyStore;
        _observability = observability;
        _logger = logger;
    }

    public async Task<PaymentResponse> CreateAsync(Guid userId, CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        var existingPending = await _paymentRepository.GetPendingByUserAndGameAsync(userId, request.GameId, cancellationToken).ConfigureAwait(false);
        if (existingPending is not null)
            throw new ConflictException("A pending payment already exists for this game.");

        var game = await _gameApiClient.GetGameAsync(request.GameId, cancellationToken).ConfigureAwait(false);
        if (game is null)
            throw new NotFoundException($"Game {request.GameId} not found.");

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GameId = request.GameId,
            Amount = game.Price,
            Currency = request.Currency,
            Provider = _gateway.ProviderName,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _gateway.AuthorizeAsync(payment.Id, payment.Amount, payment.Currency, cancellationToken).ConfigureAwait(false);
        if (result.Success && result.ProviderReference is not null)
            payment.ProviderReference = result.ProviderReference;
        if (!result.Success)
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = result.ErrorMessage ?? "Gateway authorization failed.";
        }

        await _paymentRepository.AddAsync(payment, cancellationToken).ConfigureAwait(false);
        await AppendAuditAsync("Payment", payment.Id.ToString(), "Created", null, Serialize(payment), _observability.TraceId, _observability.CorrelationId, cancellationToken).ConfigureAwait(false);
        return ToResponse(payment);
    }

    public async Task<PaymentResponse?> GetByIdAsync(Guid id, Guid? userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (payment is null) return null;
        if (!isAdmin && payment.UserId != userId)
            return null;
        return ToResponse(payment);
    }

    public async Task<PagedResponse<PaymentResponse>> GetMyPaymentsAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var skip = (pageNumber - 1) * pageSize;
        var items = await _paymentRepository.GetByUserIdAsync(userId, skip, pageSize, cancellationToken).ConfigureAwait(false);
        var total = await _paymentRepository.CountByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        var totalPages = pageSize > 0 ? (int)Math.Ceiling(total / (double)pageSize) : 0;
        return new PagedResponse<PaymentResponse>
        {
            Items = items.Select(ToResponse).ToList(),
            TotalCount = total,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            HasPreviousPage = pageNumber > 1,
            HasNextPage = pageNumber < totalPages
        };
    }

    public async Task<PaymentResponse?> ConfirmAsync(Guid paymentId, Guid? userId, bool isAdmin, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken).ConfigureAwait(false);
        if (payment is null) return null;
        if (!isAdmin && payment.UserId != userId)
            return null;

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existingId = await _idempotencyStore.GetResultAsync(idempotencyKey, cancellationToken).ConfigureAwait(false);
            if (existingId is not null && Guid.TryParse(existingId, out var existingGuid) && existingGuid == paymentId)
                return ToResponse(payment);
        }

        if (payment.Status == PaymentStatus.Paid)
        {
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
                await _idempotencyStore.SetResultAsync(idempotencyKey, paymentId.ToString(), cancellationToken).ConfigureAwait(false);
            return ToResponse(payment);
        }

        if (payment.Status != PaymentStatus.Pending && payment.Status != PaymentStatus.Authorized)
            throw new ConflictException($"Payment cannot be confirmed from status {payment.Status}.");

        var refToUse = payment.ProviderReference ?? payment.Id.ToString();
        var result = await _gateway.CaptureAsync(refToUse, cancellationToken).ConfigureAwait(false);
        var oldData = Serialize(payment);
        if (result.Success)
        {
            payment.Status = PaymentStatus.Paid;
            payment.ConfirmedAt = DateTime.UtcNow;
            payment.FailureReason = null;
        }
        else
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = result.ErrorMessage ?? "Capture failed.";
        }
        payment.UpdatedAt = DateTime.UtcNow;
        await _paymentRepository.UpdateAsync(payment, cancellationToken).ConfigureAwait(false);
        await AppendAuditAsync("Payment", payment.Id.ToString(), "Confirm", oldData, Serialize(payment), _observability.TraceId, _observability.CorrelationId, cancellationToken).ConfigureAwait(false);
        await PublishNotificationAsync(payment, cancellationToken).ConfigureAwait(false);
        if (payment.Status == PaymentStatus.Paid)
        {
            try
            {
                await _gameApiClient.AddToLibraryAsync(payment.UserId, payment.GameId, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddToLibrary failed for payment {PaymentId}, user {UserId}, game {GameId}. Compensate manually if needed.", payment.Id, payment.UserId, payment.GameId);
            }
        }
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
            await _idempotencyStore.SetResultAsync(idempotencyKey, paymentId.ToString(), cancellationToken).ConfigureAwait(false);
        return ToResponse(payment);
    }

    public async Task<PaymentResponse?> FailAsync(Guid paymentId, Guid? userId, bool isAdmin, string? failureReason, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken).ConfigureAwait(false);
        if (payment is null) return null;
        if (!isAdmin && payment.UserId != userId)
            return null;

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existingId = await _idempotencyStore.GetResultAsync(idempotencyKey, cancellationToken).ConfigureAwait(false);
            if (existingId is not null && Guid.TryParse(existingId, out var existingGuid) && existingGuid == paymentId)
                return ToResponse(payment);
        }

        if (payment.Status == PaymentStatus.Failed)
        {
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
                await _idempotencyStore.SetResultAsync(idempotencyKey, paymentId.ToString(), cancellationToken).ConfigureAwait(false);
            return ToResponse(payment);
        }

        if (payment.Status != PaymentStatus.Pending && payment.Status != PaymentStatus.Authorized)
            throw new ConflictException($"Payment cannot be failed from status {payment.Status}.");

        var refToUse = payment.ProviderReference ?? payment.Id.ToString();
        await _gateway.FailAsync(refToUse, failureReason, cancellationToken).ConfigureAwait(false);
        var oldData = Serialize(payment);
        payment.Status = PaymentStatus.Failed;
        payment.FailureReason = failureReason ?? "Failed by request.";
        payment.UpdatedAt = DateTime.UtcNow;
        await _paymentRepository.UpdateAsync(payment, cancellationToken).ConfigureAwait(false);
        await AppendAuditAsync("Payment", payment.Id.ToString(), "Fail", oldData, Serialize(payment), _observability.TraceId, _observability.CorrelationId, cancellationToken).ConfigureAwait(false);
        await PublishNotificationAsync(payment, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
            await _idempotencyStore.SetResultAsync(idempotencyKey, paymentId.ToString(), cancellationToken).ConfigureAwait(false);
        return ToResponse(payment);
    }

    public async Task<bool> ProcessWebhookAsync(WebhookProviderRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ProviderReference))
            return false;
        var payment = await _paymentRepository.GetByProviderReferenceAsync(request.ProviderReference, cancellationToken).ConfigureAwait(false);
        if (payment is null && Guid.TryParse(request.ProviderReference, out var id))
            payment = await _paymentRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (payment is null) return false;

        var newStatus = ParseStatus(request.Status);
        if (newStatus is null) return true;
        if (payment.Status == PaymentStatus.Paid || payment.Status == PaymentStatus.Failed)
            return true;

        var oldData = Serialize(payment);
        payment.Status = newStatus.Value;
        if (newStatus == PaymentStatus.Paid)
            payment.ConfirmedAt = DateTime.UtcNow;
        payment.UpdatedAt = DateTime.UtcNow;
        await _paymentRepository.UpdateAsync(payment, cancellationToken).ConfigureAwait(false);
        await AppendAuditAsync("Payment", payment.Id.ToString(), "Webhook", oldData, Serialize(payment), _observability.TraceId, _observability.CorrelationId, cancellationToken).ConfigureAwait(false);
        await PublishNotificationAsync(payment, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static PaymentStatus? ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return null;
        return status.ToUpperInvariant() switch
        {
            "PAID" or "CAPTURED" => PaymentStatus.Paid,
            "FAILED" or "DECLINED" => PaymentStatus.Failed,
            "AUTHORIZED" => PaymentStatus.Authorized,
            "CANCELLED" => PaymentStatus.Cancelled,
            "REFUNDED" => PaymentStatus.Refunded,
            _ => null
        };
    }

    private async Task PublishNotificationAsync(Payment payment, CancellationToken cancellationToken)
    {
        var (email, name) = await _userInfoService.GetUserInfoAsync(payment.UserId, cancellationToken).ConfigureAwait(false);
        var game = await _gameApiClient.GetGameAsync(payment.GameId, cancellationToken).ConfigureAwait(false);
        var evt = new PaymentNotificationEvent(
            TemplateName: payment.Status == PaymentStatus.Paid ? "PaymentApproved" : "PaymentFailed",
            UserId: payment.UserId,
            UserEmail: email,
            UserName: name,
            GameId: payment.GameId,
            GameTitle: game?.Title,
            Amount: payment.Amount,
            Currency: payment.Currency,
            PaymentStatus: payment.Status.ToString(),
            OccurredAt: DateTime.UtcNow,
            PaymentId: payment.Id,
            TraceId: _observability.TraceId,
            CorrelationId: _observability.CorrelationId
        );
        await _eventPublisher.PublishAsync(EventNames.PaymentNotification, evt, cancellationToken).ConfigureAwait(false);
    }

    private async Task AppendAuditAsync(string aggregateType, string aggregateId, string action, string? oldData, string? newData, string? traceId, string? correlationId, CancellationToken cancellationToken)
    {
        await _auditLogRepository.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            Action = action,
            OldData = oldData,
            NewData = newData,
            TraceId = traceId,
            CorrelationId = correlationId,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken).ConfigureAwait(false);
    }

    private static string Serialize(Payment p) => JsonSerializer.Serialize(new { p.Id, p.UserId, p.GameId, p.Amount, p.Currency, p.Provider, p.ProviderReference, Status = p.Status.ToString(), p.FailureReason, p.CreatedAt, p.UpdatedAt, p.ConfirmedAt }, JsonOptions);
    private static PaymentResponse ToResponse(Payment p) => new()
    {
        Id = p.Id,
        UserId = p.UserId,
        GameId = p.GameId,
        Amount = p.Amount,
        Currency = p.Currency,
        Provider = p.Provider,
        ProviderReference = p.ProviderReference,
        Status = p.Status.ToString(),
        FailureReason = p.FailureReason,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt,
        ConfirmedAt = p.ConfirmedAt
    };
}
