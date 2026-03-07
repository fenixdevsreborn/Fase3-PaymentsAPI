namespace ms_payments.Events
{
  public class NotificationEvent
  {
    public string EventType => "EMAIL_NOTIFICATION";

    public required string Title { get; set; }

    public required string Subtitle { get; set; }

    public required string Body { get; set; }

    public required string Recipient { get; set; }

    public string? Sender { get; set; }
  }
}
