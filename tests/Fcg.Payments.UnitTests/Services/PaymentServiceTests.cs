using Fcg.Payments.Application.Exceptions;
using Fcg.Payments.Application.Services;
using Fcg.Payments.Contracts.Payments;
using Fcg.Payments.Domain.Entities;
using Fcg.Payments.Domain.Enums;
using Fcg.Payments.Domain.Repositories;
using Fcg.Payments.Application.Observability;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fcg.Payments.UnitTests.Services;

public class PaymentServiceTests
{
    private readonly Mock<IPaymentRepository> _paymentRepo = new();
    private readonly Mock<IAuditLogRepository> _auditRepo = new();
    private readonly Mock<IOutboxRepository> _outboxRepo = new();
    private readonly Mock<IPaymentGateway> _gateway = new();
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly Mock<IGameApiClient> _gameClient = new();
    private readonly Mock<IUserInfoService> _userInfo = new();
    private readonly Mock<IIdempotencyStore> _idempotency = new();
    private readonly Mock<IObservabilityContextAccessor> _observability = new();
    private readonly Mock<ILogger<PaymentService>> _logger = new();

    private PaymentService CreateSut()
    {
        _observability.Setup(x => x.TraceId).Returns((string?)null);
        _observability.Setup(x => x.CorrelationId).Returns((string?)null);
        return new PaymentService(
            _paymentRepo.Object,
            _auditRepo.Object,
            _outboxRepo.Object,
            _gateway.Object,
            _eventPublisher.Object,
            _gameClient.Object,
            _userInfo.Object,
            _idempotency.Object,
            _observability.Object,
            _logger.Object);
    }

    [Fact]
    public async Task CreateAsync_WhenGameNotFound_ThrowsNotFoundException()
    {
        _gameClient.Setup(x => x.GetGameAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameInfo?)null);
        var sut = CreateSut();
        var request = new CreatePaymentRequest { GameId = Guid.NewGuid(), Currency = "BRL" };

        await Assert.ThrowsAsync<NotFoundException>(() => sut.CreateAsync(Guid.NewGuid(), request, default));
    }

    [Fact]
    public async Task CreateAsync_WhenPendingExistsForSameUserAndGame_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        _paymentRepo.Setup(x => x.GetPendingByUserAndGameAsync(userId, gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Payment { Id = Guid.NewGuid(), UserId = userId, GameId = gameId, Status = PaymentStatus.Pending });

        var sut = CreateSut();
        var request = new CreatePaymentRequest { GameId = gameId, Currency = "BRL" };

        await Assert.ThrowsAsync<ConflictException>(() => sut.CreateAsync(userId, request, default));
        _gameClient.Verify(x => x.GetGameAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenGameExists_ReturnsPaymentWithUserIdFromArgument()
    {
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        _paymentRepo.Setup(x => x.GetPendingByUserAndGameAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);
        _gameClient.Setup(x => x.GetGameAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GameInfo(gameId, "Game", 29.99m, true));
        _gateway.Setup(x => x.AuthorizeAsync(It.IsAny<Guid>(), 29.99m, "BRL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayResult(true, "ref-1", null));
        Payment? captured = null;
        _paymentRepo.Setup(x => x.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, CancellationToken>((p, _) => captured = p)
            .ReturnsAsync((Payment p, CancellationToken _) => p);

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
        _paymentRepo.Setup(x => x.GetByIdAsync(paymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Payment { Id = paymentId, UserId = ownerId });

        var sut = CreateSut();
        var result = await sut.GetByIdAsync(paymentId, otherUserId, false, default);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WhenAdmin_ReturnsPayment()
    {
        var paymentId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        _paymentRepo.Setup(x => x.GetByIdAsync(paymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Payment { Id = paymentId, UserId = ownerId, Status = PaymentStatus.Pending });

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
        _paymentRepo.Setup(x => x.GetByIdAsync(paymentId, It.IsAny<CancellationToken>())).ReturnsAsync(payment);

        var sut = CreateSut();
        var result = await sut.ConfirmAsync(paymentId, userId, false, null, default);

        Assert.NotNull(result);
        Assert.Equal("Paid", result.Status);
        _gateway.Verify(x => x.CaptureAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FailAsync_WhenNotOwner_ReturnsNull()
    {
        var paymentId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        _paymentRepo.Setup(x => x.GetByIdAsync(paymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Payment { Id = paymentId, UserId = ownerId });

        var sut = CreateSut();
        var result = await sut.FailAsync(paymentId, Guid.NewGuid(), false, "reason", null, default);

        Assert.Null(result);
    }
}
