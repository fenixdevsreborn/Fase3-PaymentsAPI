using Amazon.Lambda.SQSEvents;
using System.Text.Json;
using ms_payments.Events;
using ms_payments.Services;

namespace ms_payments.Handlers;

public class PaymentHandler
{
  private readonly PaymentService _paymentService;

  public PaymentHandler()
  {
    _paymentService = new PaymentService();
  }

  public async Task HandleAsync(SQSEvent evnt)
  {
    foreach (var record in evnt.Records)
    {
      try
      {
        var purchase = JsonSerializer.Deserialize<PurchaseRequestedEvent>(record.Body);

        if (purchase == null)
          throw new Exception("Evento inválido.");

        await _paymentService.ProcessPaymentAsync(purchase);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Erro ao processar pagamento: {ex.Message}");
        throw; 
      }
    }
  }
}