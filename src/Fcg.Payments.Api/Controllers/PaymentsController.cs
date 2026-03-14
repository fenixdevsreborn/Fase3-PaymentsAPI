using Fcg.Payments.Api.Authentication;
using Fcg.Payments.Api.Authorization;
using Fcg.Payments.Api.Observability;
using Fcg.Payments.Application.Exceptions;
using Fcg.Payments.Application.Services;
using Fcg.Payments.Contracts.Audit;
using Fcg.Payments.Contracts.Paging;
using Fcg.Payments.Contracts.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fcg.Payments.Api.Controllers;

[ApiController]
[Route("payments")]
[Authorize(Policy = FcgPolicies.RequireAuthenticatedUser)]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IAuditService _auditService;
    private readonly FcgMeters _meters;

    public PaymentsController(IPaymentService paymentService, IAuditService auditService, FcgMeters meters)
    {
        _paymentService = paymentService;
        _auditService = auditService;
        _meters = meters;
    }

    [HttpPost]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentResponse>> Create([FromBody] CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized();
        try
        {
            var payment = await _paymentService.CreateAsync(userId.Value, request, cancellationToken).ConfigureAwait(false);
            _meters.RecordPaymentCreated();
            return CreatedAtAction(nameof(GetById), new { id = payment.Id }, payment);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsAdmin();
        var payment = await _paymentService.GetByIdAsync(id, userId, isAdmin, cancellationToken).ConfigureAwait(false);
        if (payment is null)
            return NotFound();
        return Ok(payment);
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(PagedResponse<PaymentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<PaymentResponse>>> GetMe([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized();
        var result = await _paymentService.GetMyPaymentsAsync(userId.Value, pageNumber, pageSize, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PaymentResponse>> Confirm(Guid id, [FromBody] ConfirmPaymentRequest? request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsAdmin();
        try
        {
            var payment = await _paymentService.ConfirmAsync(id, userId, isAdmin, request?.IdempotencyKey, cancellationToken).ConfigureAwait(false);
            if (payment is null)
                return NotFound();
            if (string.Equals(payment.Status, "Paid", StringComparison.OrdinalIgnoreCase))
                _meters.RecordPaymentPaid();
            return Ok(payment);
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/fail")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PaymentResponse>> Fail(Guid id, [FromBody] FailPaymentRequest? request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsAdmin();
        try
        {
            var payment = await _paymentService.FailAsync(id, userId, isAdmin, request?.FailureReason, request?.IdempotencyKey, cancellationToken).ConfigureAwait(false);
            if (payment is null)
                return NotFound();
            if (string.Equals(payment.Status, "Failed", StringComparison.OrdinalIgnoreCase))
                _meters.RecordPaymentFailed();
            return Ok(payment);
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}/audit")]
    [ProducesResponseType(typeof(IReadOnlyList<AuditEntryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AuditEntryResponse>>> GetAudit(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsAdmin();
        var entries = await _auditService.GetPaymentAuditAsync(id, userId, isAdmin, cancellationToken).ConfigureAwait(false);
        if (entries.Count == 0)
        {
            var payment = await _paymentService.GetByIdAsync(id, userId, isAdmin, cancellationToken).ConfigureAwait(false);
            if (payment is null)
                return NotFound();
        }
        return Ok(entries);
    }
}
