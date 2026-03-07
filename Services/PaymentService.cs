using ms_payments.Events;
using ms_payments.Messaging;
using Amazon.SQS;

namespace ms_payments.Services;

public class PaymentService
{
  private readonly EventPublisher _publisher;

  public PaymentService()
  {
    var sqs = new AmazonSQSClient();
    _publisher = new EventPublisher(sqs);
  }

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

    var paymentProcessed = new PaymentProcessedEvent
    {
      UserId = payment.UserId,
      GameId = payment.GameId,
      Amount = payment.Amount,
      Status = status,
      ProcessedAt = DateTime.UtcNow
    };

    var gamesQueue = Environment.GetEnvironmentVariable("GAMES_QUEUE_URL");

    await _publisher.PublishAsync(gamesQueue, paymentProcessed);

    var notificationEvent = new NotificationEvent
    {
      Title = "Resultado da compra",
      Subtitle = $"Compra do jogo {payment.GameId}",
      Body = status == "APPROVED"
        ? "Seu pagamento foi aprovado!"
        : "Seu pagamento foi recusado.",
      Recipient = payment.UserId
    };

    var notificationQueue = Environment.GetEnvironmentVariable("NOTIFICATION_QUEUE_URL");

    await _publisher.PublishAsync(notificationQueue, notificationEvent);
  }
}