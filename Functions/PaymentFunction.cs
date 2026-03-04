using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using ms_payments.Handlers;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ms_payments.Functions;

public class PaymentFunction
{
  private readonly PaymentHandler _handler;

  public PaymentFunction()
  {
    _handler = new PaymentHandler();
  }

  public async Task FunctionHandler(SQSEvent evnt)
  {
    await _handler.HandleAsync(evnt);
  }
}