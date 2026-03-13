using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fcg.Payments.Infrastructure.Outbox;

internal sealed class OutboxSqsSender : IOutboxSqsSender
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly IAmazonSQS _sqs;
    private readonly OutboxRelayOptions _options;
    private readonly ILogger<OutboxSqsSender> _logger;

    public OutboxSqsSender(IAmazonSQS sqs, IOptions<OutboxRelayOptions> options, ILogger<OutboxSqsSender> logger)
    {
        _sqs = sqs;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(SqsNotificationMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_options.QueueUrl))
            throw new InvalidOperationException("OutboxRelay:QueueUrl is not configured.");

        var body = JsonSerializer.Serialize(message, JsonOptions);
        var request = new SendMessageRequest
        {
            QueueUrl = _options.QueueUrl,
            MessageBody = body
        };
        await _sqs.SendMessageAsync(request, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("OutboxSqsSender: sent MessageId={MessageId} to SQS", message.MessageId);
    }
}
