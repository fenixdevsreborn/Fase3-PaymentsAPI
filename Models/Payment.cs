using Amazon.DynamoDBv2.DataModel;

namespace ms_payments.Models
{
  [DynamoDBTable("Payments")]
  public class Payment
  {
    [DynamoDBHashKey]
    public string PaymentId { get; set; }

    public string UserId { get; set; }

    public string GameId { get; set; }

    public string Email { get; set; }

    public decimal Amount { get; set; }

    public decimal GameValue { get; set; }

    public string Status { get; set; }

    public DateTime CreatedAt { get; set; }
  }
}
