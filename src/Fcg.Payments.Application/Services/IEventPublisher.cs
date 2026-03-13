namespace Fcg.Payments.Application.Services;

/// <summary>Publishes integration events (outbox). Downstream can send to queue for email/Lambda.</summary>
public interface IEventPublisher
{
    Task PublishAsync(string eventName, object payload, CancellationToken cancellationToken = default);
}
