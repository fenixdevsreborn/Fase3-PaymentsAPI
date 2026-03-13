using System.ComponentModel.DataAnnotations;

namespace Fcg.Payments.Contracts.Payments;

public class CreatePaymentRequest
{
    public Guid GameId { get; set; }
    public string Currency { get; set; } = "BRL";
}
