namespace Fcg.Payments.Infrastructure.Outbox;

internal interface IOutboxSqsSender
{
    Task SendAsync(SqsNotificationMessage message, CancellationToken cancellationToken = default);
}
