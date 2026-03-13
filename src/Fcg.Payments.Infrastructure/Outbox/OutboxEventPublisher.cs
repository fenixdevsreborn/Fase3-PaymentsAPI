using System.Text.Json;
using Fcg.Payments.Application.Services;
using Fcg.Payments.Domain.Entities;
using Fcg.Payments.Domain.Repositories;

namespace Fcg.Payments.Infrastructure.Outbox;

public class OutboxEventPublisher : IEventPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly IOutboxRepository _outbox;

    public OutboxEventPublisher(IOutboxRepository outbox)
    {
        _outbox = outbox;
    }

    public async Task PublishAsync(string eventName, object payload, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        await _outbox.AddAsync(new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EventName = eventName,
            Payload = json,
            Status = "Pending",
            RetryCount = 0,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken).ConfigureAwait(false);
    }
}
