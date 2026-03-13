using System.ComponentModel.DataAnnotations;

namespace Fcg.Payments.Contracts.Payments;

public class FailPaymentRequest
{
    [MaxLength(500)]
    public string? FailureReason { get; set; }
    public string? IdempotencyKey { get; set; }
}
