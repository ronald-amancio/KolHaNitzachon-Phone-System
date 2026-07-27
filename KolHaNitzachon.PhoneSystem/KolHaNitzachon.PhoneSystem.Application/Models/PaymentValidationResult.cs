namespace KolHaNitzachon.PhoneSystem.Application.Payments.Models;

public sealed record PaymentValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public static PaymentValidationResult Success { get; } = new(true, Array.Empty<string>());
}
