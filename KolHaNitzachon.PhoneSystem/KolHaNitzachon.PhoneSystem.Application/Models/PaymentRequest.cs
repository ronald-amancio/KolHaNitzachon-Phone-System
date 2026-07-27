namespace KolHaNitzachon.PhoneSystem.Application.Payments.Models;

public sealed record PaymentRequest
{
    public required decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public required string InvoiceNumber { get; init; }
    public required string PaymentToken { get; init; }
    public string? CvvToken { get; init; }
    public string? CardholderName { get; init; }
    public string? BillingZipCode { get; init; }
    public string? Description { get; init; }
    public required string IdempotencyKey { get; init; }
    public string? CallSid { get; init; }
    public Guid? RecipientId { get; init; }
}
