using System.Text.Json;
using Fcg.Payments.Application.Constants;
using Fcg.Payments.Contracts.Events;
using Fcg.Payments.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fcg.Payments.Infrastructure.Outbox;

public sealed class OutboxRelayOptions
{
    public const string SectionName = "OutboxRelay";
    public string? QueueUrl { get; set; }
    public int IntervalSeconds { get; set; } = 10;
    public int BatchSize { get; set; } = 20;
    public int MaxRetries { get; set; } = 5;
}

public sealed class OutboxRelayService : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxRelayService> _logger;
    private readonly OutboxRelayOptions _options;

    public OutboxRelayService(IServiceProvider serviceProvider, ILogger<OutboxRelayService> logger, IOptions<OutboxRelayOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrEmpty(_options.QueueUrl))
        {
            _logger.LogWarning("OutboxRelay: QueueUrl not configured; relay disabled.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RelayPendingAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxRelay iteration failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.IntervalSeconds), stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task RelayPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var sqs = scope.ServiceProvider.GetRequiredService<IOutboxSqsSender>();

        var pending = await outbox.GetPendingAsync(_options.BatchSize, cancellationToken).ConfigureAwait(false);
        if (pending.Count == 0) return;

        foreach (var evt in pending)
        {
            if (evt.EventName != EventNames.PaymentNotification)
            {
                await outbox.MarkFailedAsync(evt.Id, cancellationToken).ConfigureAwait(false);
                continue;
            }

            PaymentNotificationEvent? payload;
            try
            {
                payload = JsonSerializer.Deserialize<PaymentNotificationEvent>(evt.Payload, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OutboxRelay: failed to deserialize event {EventId}", evt.Id);
                await outbox.MarkFailedAsync(evt.Id, cancellationToken).ConfigureAwait(false);
                continue;
            }

            if (payload == null)
            {
                await outbox.MarkFailedAsync(evt.Id, cancellationToken).ConfigureAwait(false);
                continue;
            }

            var (templateName, payloadType) = payload.TemplateName == "PaymentApproved"
                ? ("PaymentApproved", "PaymentApprovedEmailPayload")
                : ("PaymentFailed", "PaymentFailedEmailPayload");

            var emailPayload = new
            {
                payload.UserId,
                payload.UserName,
                payload.UserEmail,
                payload.GameId,
                payload.GameTitle,
                payload.Amount,
                payload.Currency,
                payload.PaymentId,
                payload.PaymentStatus,
                PurchaseDate = payload.OccurredAt
            };

            var message = new SqsNotificationMessage
            {
                MessageId = evt.Id.ToString(),
                TemplateName = templateName,
                Channel = 0,
                Recipient = payload.UserEmail ?? "",
                CorrelationId = payload.CorrelationId,
                TraceId = payload.TraceId,
                OccurredAt = payload.OccurredAt,
                PayloadType = payloadType,
                Payload = JsonSerializer.Serialize(emailPayload, JsonOptions)
            };

            try
            {
                await sqs.SendAsync(message, cancellationToken).ConfigureAwait(false);
                await outbox.MarkProcessedAsync(evt.Id, cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("OutboxRelay: event {EventId} sent to SQS", evt.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OutboxRelay: failed to send event {EventId}", evt.Id);
                await outbox.IncrementRetryAsync(evt.Id, cancellationToken).ConfigureAwait(false);
                if (evt.RetryCount + 1 >= _options.MaxRetries)
                    await outbox.MarkFailedAsync(evt.Id, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
