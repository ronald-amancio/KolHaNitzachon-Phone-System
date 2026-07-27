using KolHaNitzachon.PhoneSystem.API.Contracts.Payments;
using KolHaNitzachon.PhoneSystem.Application.Payments.Interfaces;
using KolHaNitzachon.PhoneSystem.Application.Payments.Models;
using KolHaNitzachon.PhoneSystem.Domain.Payments;
using Microsoft.AspNetCore.Mvc;

namespace KolHaNitzachon.PhoneSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PaymentsController : ControllerBase
{
    private readonly IPaymentGateway _paymentGateway;
    public PaymentsController(IPaymentGateway paymentGateway) => _paymentGateway = paymentGateway;

    [HttpPost("process")]
    public async Task<ActionResult<PaymentResult>> Process([FromBody] ProcessPaymentRequest request, CancellationToken cancellationToken)
    {
        var result = await _paymentGateway.ProcessPaymentAsync(new PaymentRequest
        {
            Amount = request.Amount, Currency = "USD", InvoiceNumber = request.InvoiceNumber.Trim(), PaymentToken = request.PaymentToken.Trim(),
            CvvToken = request.CvvToken?.Trim(), CardholderName = request.CardholderName?.Trim(), BillingZipCode = request.BillingZipCode?.Trim(),
            Description = request.Description?.Trim(), IdempotencyKey = request.IdempotencyKey.Trim(), CallSid = request.CallSid?.Trim(), RecipientId = request.RecipientId
        }, cancellationToken);

        return result.Outcome switch
        {
            PaymentOutcome.Approved or PaymentOutcome.Declined => Ok(result),
            PaymentOutcome.ValidationFailed => BadRequest(result),
            PaymentOutcome.GatewayTimeout => StatusCode(StatusCodes.Status504GatewayTimeout, result),
            _ => StatusCode(StatusCodes.Status502BadGateway, result)
        };
    }
}
