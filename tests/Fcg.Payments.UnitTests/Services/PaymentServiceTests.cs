using Fcg.Payments.Application.Exceptions;
using Fcg.Payments.Application.Services;
using Fcg.Payments.Contracts.Payments;
using Fcg.Payments.Domain.Entities;
using Fcg.Payments.Domain.Enums;
using Fcg.Payments.Domain.Repositories;
using Fcg.Payments.Application.Observability;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Fcg.Payments.UnitTests.Services;

public class PaymentServiceTests
{
    private readonly IPaymentRepository _paymentRepo = Substitute.For<IPaymentRepository>();
    private readonly IAuditLogRepository _auditRepo = Substitute.For<IAuditLogRepository>();
    private readonly IOutboxRepository _outboxRepo = Substitute.For<IOutboxRepository>();
    private readonly IPaymentGateway _gateway = Substitute.For<IPaymentGateway>();
    private readonly IEventPublisher _eventPublisher = Substitute.For<IEventPublisher>();
    private readonly IGameApiClient _gameClient = Substitute.For<IGameApiClient>();
    private readonly IUserInfoService _userInfo = Substitute.For<IUserInfoService>();
    private readonly IIdempotencyStore _idempotency = Substitute.For<IIdempotencyStore>();
    private readonly IObservabilityContextAccessor _observability = Substitute.For<IObservabilityContextAccessor>();
    private readonly ILogger<PaymentService> _logger = Substitute.For<ILogger<PaymentService>>();

    private PaymentService CreateSut()
    {
        _observability.TraceId.Returns((string?)null);
        _observability.CorrelationId.Returns((string?)null);
        return new PaymentService(
            _paymentRepo,
            _auditRepo,
            _outboxRepo,
            _gateway,
            _eventPublisher,
            _gameClient,
            _userInfo,
            _idempotency,
            _observability,
            _logger);
    }

    [Fact]
    public async Task CreateAsync_WhenGameNotFound_ThrowsNotFoundException()
    {
        _gameClient.GetGameAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((GameInfo?)null);
        var sut = CreateSut();
        var request = new CreatePaymentRequest { GameId = Guid.NewGuid(), Currency = "BRL" };

        await Assert.ThrowsAsync<NotFoundException>(() => sut.CreateAsync(Guid.NewGuid(), request, default));
    }

    [Fact]
    public async Task CreateAsync_WhenPendingExistsForSameUserAndGame_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        _paymentRepo.GetPendingByUserAndGameAsync(userId, gameId, Arg.Any<CancellationToken>())
            .Returns(new Payment { Id = Guid.NewGuid(), UserId = userId, GameId = gameId, Status = PaymentStatus.Pending });

        var sut = CreateSut();
        var request = new CreatePaymentRequest { GameId = gameId, Currency = "BRL" };

        await Assert.ThrowsAsync<ConflictException>(() => sut.CreateAsync(userId, request, default));
#pragma warning disable CS4014
        _gameClient.DidNotReceive().GetGameAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
#pragma warning restore CS4014
    }

    [Fact]
    public async Task CreateAsync_WhenGameExists_ReturnsPaymentWithUserIdFromArgument()
    {
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        _paymentRepo.GetPendingByUserAndGameAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Payment?)null);
        _gameClient.GetGameAsync(gameId, Arg.Any<CancellationToken>())
            .Returns(new GameInfo(gameId, "Game", 29.99m, true));
        _gateway.AuthorizeAsync(Arg.Any<Guid>(), 29.99m, "BRL", Arg.Any<CancellationToken>())
            .Returns(new GatewayResult(true, "ref-1", null));
        Payment? captured = null;
        _paymentRepo.AddAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var p = (Payment)callInfo[0];
                captured = p;
                return p;
            });

        var sut = CreateSut();
        var request = new CreatePaymentRequest { GameId = gameId, Currency = "BRL" };
        var response = await sut.CreateAsync(userId, request, default);

        Assert.NotNull(response);
        Assert.Equal(userId, response.UserId);
        Assert.Equal(gameId, response.GameId);
        Assert.Equal(29.99m, response.Amount);
        Assert.NotNull(captured);
        Assert.Equal(userId, captured.UserId);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotOwnerAndNotAdmin_ReturnsNull()
    {
        var paymentId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        _paymentRepo.GetByIdAsync(paymentId, Arg.Any<CancellationToken>())
            .Returns(new Payment { Id = paymentId, UserId = ownerId });

        var sut = CreateSut();
        var result = await sut.GetByIdAsync(paymentId, otherUserId, false, default);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WhenAdmin_ReturnsPayment()
    {
        var paymentId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        _paymentRepo.GetByIdAsync(paymentId, Arg.Any<CancellationToken>())
            .Returns(new Payment { Id = paymentId, UserId = ownerId, Status = PaymentStatus.Pending });

        var sut = CreateSut();
        var result = await sut.GetByIdAsync(paymentId, Guid.NewGuid(), true, default);

        Assert.NotNull(result);
        Assert.Equal(paymentId, result.Id);
    }

    [Fact]
    public async Task ConfirmAsync_WhenAlreadyPaid_ReturnsSamePayment()
    {
        var paymentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var payment = new Payment { Id = paymentId, UserId = userId, Status = PaymentStatus.Paid };
        _paymentRepo.GetByIdAsync(paymentId, Arg.Any<CancellationToken>()).Returns(payment);

        var sut = CreateSut();
        var result = await sut.ConfirmAsync(paymentId, userId, false, null, default);

        Assert.NotNull(result);
        Assert.Equal("Paid", result.Status);
#pragma warning disable CS4014
        _gateway.DidNotReceive().CaptureAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
#pragma warning restore CS4014
    }

    [Fact]
    public async Task FailAsync_WhenNotOwner_ReturnsNull()
    {
        var paymentId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        _paymentRepo.GetByIdAsync(paymentId, Arg.Any<CancellationToken>())
            .Returns(new Payment { Id = paymentId, UserId = ownerId });

        var sut = CreateSut();
        var result = await sut.FailAsync(paymentId, Guid.NewGuid(), false, "reason", null, default);

        Assert.Null(result);
    }
}
