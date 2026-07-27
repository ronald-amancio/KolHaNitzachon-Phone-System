using KolHaNitzachon.PhoneSystem.Application.Payments.Models;

namespace KolHaNitzachon.PhoneSystem.Application.Payments.Interfaces;

public interface IPaymentGateway
{
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default);
}
