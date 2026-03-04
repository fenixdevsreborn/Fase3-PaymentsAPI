namespace ms_payments.Models
{
   public class PaymentEvent
   {
      public string UserId { get; set; }
      public decimal Amount { get; set; }
      public string Email { get; set; }
   }
}
