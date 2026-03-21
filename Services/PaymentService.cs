using ms_payments.Events;
using ms_payments.Messaging;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using ms_payments.Models;
using Amazon.SQS;

namespace ms_payments.Services;

public class PaymentService
{
  private readonly EventPublisher _publisher;
  private readonly DynamoDBContext _context;

  public PaymentService()
  {
    var sqs = new AmazonSQSClient();
    var dynamo = new AmazonDynamoDBClient();

    _publisher = new EventPublisher(sqs);
    _context = new DynamoDBContext(dynamo);
  }

  public async Task ProcessPaymentAsync(PurchaseRequestedEvent payment)
  {
    var paymentId = Guid.NewGuid().ToString();

    Console.WriteLine($@"==================================
      Novo pagamento recebido
      User: {payment.UserId}
      Email: {payment.Email}
      Game: {payment.GameId}
      Amount: {payment.Amount}
      GameValue: {payment.GameValue}");

    await Task.Delay(1000);

    var status = payment.Amount == payment.GameValue
        ? "APPROVED"
        : "REJECTED";

    Console.WriteLine($"STATUS: {status}");

    var paymentProcessed = new PaymentProcessedEvent
    {
      UserId = payment.UserId,
      PaymentId = paymentId,
      Email = payment.Email,
      GameId = payment.GameId,
      Amount = payment.Amount,
      Status = status,
      ProcessedAt = DateTime.UtcNow
    };

    var gamesQueue = Environment.GetEnvironmentVariable("GAMES_QUEUE_URL");

    await _publisher.PublishAsync(gamesQueue, paymentProcessed);

    var notificationEvent = new NotificationEvent
    {
      Title = $"{payment.GameName}",
      Subtitle = $"Id da compra {paymentId}",
      Body = status == "APPROVED"
        ? $"A compra no valor de R$ {payment.Amount} foi aprovada e o jogo {payment.GameName} foi adicionado a sua biblioteca de jogos"
        : "Seu pagamento foi recusado.",
      Recipient = payment.Email
    };

    var notificationQueue = Environment.GetEnvironmentVariable("NOTIFICATION_QUEUE_URL");

    await _publisher.PublishAsync(notificationQueue, notificationEvent);

    var paymentEntity = new Payment
    {
      PaymentId = paymentId,
      UserId = payment.UserId,
      GameId = payment.GameId,
      Email = payment.Email,
      Amount = payment.Amount,
      GameValue = payment.GameValue,
      Status = status,
      CreatedAt = DateTime.UtcNow
    };

    await _context.SaveAsync(paymentEntity);
  }
}