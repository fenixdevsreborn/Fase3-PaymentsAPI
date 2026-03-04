namespace ms_payments.Events
{
  public class PurchaseRequestedEvent
  {
    public string UserId { get; set; }
    public string GameId { get; set; }
    public decimal Amount { get; set; }
    public DateTime RequestedAt { get; set; }
  }
}
