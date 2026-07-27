using KolHaNitzachon.PhoneSystem.Application.Payments.Models;

namespace KolHaNitzachon.PhoneSystem.Application.Payments.Interfaces;

public interface IPaymentRequestValidator
{
    PaymentValidationResult Validate(PaymentRequest request);
}
