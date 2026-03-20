namespace ms_payments.Events
{
  public class PaymentProcessedEvent
  {
    public string EventType => "PAYMENT_PROCESSED";

    public string UserId { get; set; }

    public string PaymentId { get; set; }

    public string Email { get; set; }

    public string GameId { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; }

    public DateTime ProcessedAt { get; set; }
  }
}
