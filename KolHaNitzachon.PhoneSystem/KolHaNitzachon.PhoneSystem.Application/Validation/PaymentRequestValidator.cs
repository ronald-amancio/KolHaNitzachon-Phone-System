using KolHaNitzachon.PhoneSystem.Application.Payments.Interfaces;
using KolHaNitzachon.PhoneSystem.Application.Payments.Models;

namespace KolHaNitzachon.PhoneSystem.Application.Payments.Validation;

public sealed class PaymentRequestValidator : IPaymentRequestValidator
{
    public PaymentValidationResult Validate(PaymentRequest request)
    {
        var errors = new List<string>();
        if (request.Amount < 1m) errors.Add("Amount must be at least 1.00.");
        if (request.Amount > 100000m) errors.Add("Amount cannot exceed 100000.00.");
        if (string.IsNullOrWhiteSpace(request.Currency)) errors.Add("Currency is required.");
        else if (!request.Currency.Equals("USD", StringComparison.OrdinalIgnoreCase)) errors.Add("Only USD is currently supported.");
        if (string.IsNullOrWhiteSpace(request.InvoiceNumber)) errors.Add("Invoice number is required.");
        if (string.IsNullOrWhiteSpace(request.PaymentToken)) errors.Add("A secure payment token is required.");
        else if (LooksLikeRawCardNumber(request.PaymentToken)) errors.Add("Raw card data is not accepted. Use a secure token.");
        if (!string.IsNullOrWhiteSpace(request.CvvToken) && request.CvvToken.All(char.IsDigit) && request.CvvToken.Length is 3 or 4) errors.Add("Raw CVV is not accepted. Use a secure CVV token.");
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey)) errors.Add("Idempotency key is required.");
        return errors.Count == 0 ? PaymentValidationResult.Success : new(false, errors);
    }

    private static bool LooksLikeRawCardNumber(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length is < 13 or > 19) return false;
        var sum = 0; var alternate = false;
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var n = digits[i] - '0';
            if (alternate) { n *= 2; if (n > 9) n -= 9; }
            sum += n; alternate = !alternate;
        }
        return sum % 10 == 0;
    }
}
