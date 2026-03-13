namespace Fcg.Payments.Contracts.Events;

/// <summary>Event for email service: template + user/game/payment data. TraceId and CorrelationId for observability propagation.</summary>
public record PaymentNotificationEvent(
    string TemplateName,
    Guid UserId,
    string? UserEmail,
    string? UserName,
    Guid GameId,
    string? GameTitle,
    decimal Amount,
    string Currency,
    string PaymentStatus,
    DateTime OccurredAt,
    Guid PaymentId,
    string? TraceId = null,
    string? CorrelationId = null
);
