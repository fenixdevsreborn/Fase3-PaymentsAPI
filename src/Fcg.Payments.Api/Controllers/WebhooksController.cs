using Fcg.Payments.Application.Services;
using Fcg.Payments.Contracts.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fcg.Payments.Api.Controllers;

[ApiController]
[Route("payments")]
public class WebhooksController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public WebhooksController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost("webhooks/provider")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProviderWebhook([FromBody] WebhookProviderRequest request, CancellationToken cancellationToken)
    {
        var ok = await _paymentService.ProcessWebhookAsync(request, cancellationToken).ConfigureAwait(false);
        return ok ? Ok() : BadRequest();
    }
}
