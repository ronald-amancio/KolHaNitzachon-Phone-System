using System.ComponentModel.DataAnnotations;

namespace KolHaNitzachon.PhoneSystem.API.Contracts.Payments;

public sealed record ProcessPaymentRequest
{
    [Range(typeof(decimal), "1.00", "100000.00")] public decimal Amount { get; init; }
    [Required, StringLength(50)] public string InvoiceNumber { get; init; } = string.Empty;
    [Required, StringLength(500)] public string PaymentToken { get; init; } = string.Empty;
    [StringLength(500)] public string? CvvToken { get; init; }
    [StringLength(100)] public string? CardholderName { get; init; }
    [StringLength(20)] public string? BillingZipCode { get; init; }
    [StringLength(250)] public string? Description { get; init; }
    [Required, StringLength(150)] public string IdempotencyKey { get; init; } = string.Empty;
    [StringLength(100)] public string? CallSid { get; init; }
    public Guid? RecipientId { get; init; }
}
