using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using ms_payments.Handlers;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ms_payments;

public class Function
{
  private readonly PaymentHandler _handler;

  public Function()
  {
    _handler = new PaymentHandler();
  }

  public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
  {
    await _handler.HandleAsync(evnt);
  }
}