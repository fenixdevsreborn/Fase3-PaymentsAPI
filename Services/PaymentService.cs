using ms_payments.Events;

namespace ms_payments.Services;

public class PaymentService
{
  public async Task ProcessPaymentAsync(PurchaseRequestedEvent payment)
  {
    Console.WriteLine($@"==================================
    Novo pagamento recebido
    User: {payment.UserId}
    Game: {payment.GameId}
    Amount: {payment.Amount}");

    await Task.Delay(1000);

    var status = payment.Amount >= 100
        ? "APPROVED"
        : "REJECTED";

    Console.WriteLine($"STATUS: {status}");
    Console.WriteLine("==================================");

    //TODO - Publicação de evento PaymentProcessed
  }
}