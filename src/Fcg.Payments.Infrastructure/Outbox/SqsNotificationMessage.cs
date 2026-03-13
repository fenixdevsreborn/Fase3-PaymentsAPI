namespace Fcg.Payments.Infrastructure.Outbox;

/// <summary>Forma da mensagem enviada à fila SQS para a Lambda de notificação. Deve ser compatível com NotificationMessage do Fcg.Notification.Contracts.</summary>
internal sealed class SqsNotificationMessage
{
    public string MessageId { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public int Channel { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public string? TraceId { get; set; }
    public DateTime OccurredAt { get; set; }
    public string PayloadType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
}
