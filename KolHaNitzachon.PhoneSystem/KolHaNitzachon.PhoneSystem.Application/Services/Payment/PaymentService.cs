//using Domain.Models;
//using Domain.Services;
using KolHaNitzachon.PhoneSystem.Application.Interfaces.Payment;
using Microsoft.Extensions.Logging;

namespace KolHaNitzachon.PhoneSystem.Application.Services.Payment;

/// <summary>
/// Payment service for IVR flow - handles customer creation, payment method attachment, and payment processing
/// </summary>
public class PaymentService
{
    private readonly IPaymentGatewayService _paymentGatewayService;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentGatewayService paymentGatewayService,
        ILogger<PaymentService> logger)
    {
        _paymentGatewayService = paymentGatewayService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or creates a gateway customer ID for the user
    /// </summary>
    public async Task<string?> GetOrCreateCustomerIdAsync(
    string callSid,
    string? phoneNumber,
    string? donorName = null,
    string? email = null,
    CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(callSid))
            {
                _logger.LogError(
                    "Cannot create a payment customer because CallSid is empty.");

                return null;
            }

            var externalKey = !string.IsNullOrWhiteSpace(phoneNumber)
                ? phoneNumber
                : $"IVR-{callSid}";

            var nameParts = (donorName ?? string.Empty)
                .Trim()
                .Split(
                    ' ',
                    2,
                    StringSplitOptions.RemoveEmptyEntries);

            var firstName = nameParts.Length > 0
                ? nameParts[0]
                : "IVR";

            var lastName = nameParts.Length > 1
                ? nameParts[1]
                : "Donor";

            var request = new UpsertCustomerRequest(
                ExternalKey: externalKey,
                Email: email ?? string.Empty,
                FirstName: firstName,
                LastName: lastName,
                Phone: phoneNumber ?? string.Empty,
                IdempotencyKey: $"customer-{callSid}",
                Metadata: Metadata.Empty());

            var result = await _paymentGatewayService
                .UpsertCustomerAsync(request, ct);

            if (!result.Success)
            {
                _logger.LogError(
                    "Failed to create or retrieve payment customer. " +
                    "CallSid={CallSid}, Error={Error}",
                    callSid,
                    result.ErrorMessage);

                return null;
            }

            _logger.LogInformation(
                "Created or retrieved payment customer. " +
                "CallSid={CallSid}, CustomerId={CustomerId}",
                callSid,
                result.Value.CustomerId);

            return result.Value.CustomerId;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Customer creation was cancelled. CallSid={CallSid}",
                callSid);

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error creating payment customer. " +
                "CallSid={CallSid}",
                callSid);

            return null;
        }
    }

    /// <summary>
    /// Attaches a payment method (card token) to a customer
    /// </summary>
    public async Task<string?> AttachPaymentMethodAsync(
        string customerId,
        string token,
        string? expiryMonth = null,
        string? expiryYear = null,
        CancellationToken ct = default)
    {
        try
        {
            var metadata = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(expiryMonth) && !string.IsNullOrWhiteSpace(expiryYear))
            {
                metadata["ExpiryMMYY"] = $"{expiryMonth.PadLeft(2, '0')}{expiryYear.Substring(Math.Max(0, expiryYear.Length - 2))}";
            }
            metadata["TokenAlias"] = "IVR Phone Order";

            var request = new AttachPaymentMethodRequest(
                new CustomerRef(customerId),
                PaymentMethodType.Card,
                token,
                SetAsDefault: true,
                IdempotencyKey: Guid.NewGuid().ToString(),
                Metadata.From(metadata)
            );

            var result = await _paymentGatewayService.AttachPaymentMethodAsync(request, ct);
            
            if (result.Success)
            {
                _logger.LogInformation("Attached payment method: {PaymentMethodId}", result.Value.PaymentMethodId);
                return result.Value.PaymentMethodId;
            }

            _logger.LogError("Failed to attach payment method: {Error}", result.ErrorMessage);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error attaching payment method");
            return null;
        }
    }

    /// <summary>
    /// Tokenizes raw card data via the gateway and attaches the resulting token as a payment method.
    /// </summary>
    public async Task<(string? PaymentMethodId, string? ErrorMessage)> TokenizeAndAttachPaymentMethodAsync(
        string customerId,
        string cardNumber,
        string expMMYY,
        string cvv,
        string billingZip,
        CancellationToken ct = default)
    {
        try
        {
            var tokenResult = await _paymentGatewayService.TokenizeCardAsync(
                new TokenizeCardRequest(cardNumber, expMMYY, cvv, billingZip), ct);

            if (!tokenResult.Success)
            {
                _logger.LogError("Card tokenization failed: {Error}", tokenResult.ErrorMessage);
                return (null, tokenResult.ErrorMessage);
            }

            var metadata = new Dictionary<string, string>
            {
                ["ExpiryMMYY"] = expMMYY,
                ["TokenAlias"] = "IVR Phone Order"
            };

            var attachRequest = new AttachPaymentMethodRequest(
                new CustomerRef(customerId),
                PaymentMethodType.Card,
                tokenResult.Value.Token,
                SetAsDefault: false, //true
                IdempotencyKey: Guid.NewGuid().ToString(),
                Metadata.From(metadata)
            );

            var attachResult = await _paymentGatewayService.AttachPaymentMethodAsync(attachRequest, ct);

            if (attachResult.Success)
            {
                _logger.LogInformation("Tokenized and attached payment method: {PaymentMethodId}", attachResult.Value.PaymentMethodId);
                return (attachResult.Value.PaymentMethodId, null);
            }

            _logger.LogError("Failed to attach tokenized payment method: {Error}", attachResult.ErrorMessage);
            return (null, attachResult.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tokenizing and attaching payment method");
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// Lists saved payment methods for a customer
    /// </summary>
    public async Task<IReadOnlyList<PaymentMethodView>> ListPaymentMethodsAsync(string customerId, CancellationToken ct = default)
    {
        try
        {
            var request = new ListPaymentMethodsRequest(new CustomerRef(customerId));
            var result = await _paymentGatewayService.ListPaymentMethodsAsync(request, ct);
            
            if (result.Success)
            {
                return result.Value;
            }

            _logger.LogWarning("Failed to list payment methods: {Error}", result.ErrorMessage);
            return Array.Empty<PaymentMethodView>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing payment methods");
            return Array.Empty<PaymentMethodView>();
        }
    }

    /// <summary>
    /// Tokenizes raw card data via the gateway and returns the token without attaching it to any customer.
    /// Use this when you need consent before saving the card.
    /// </summary>
    public async Task<(string? Token, string? ErrorMessage)> TokenizeCardOnlyAsync(
        string cardNumber,
        string expMMYY,
        string cvv,
        string billingZip,
        CancellationToken ct = default)
    {
        try
        {
            var tokenResult = await _paymentGatewayService.TokenizeCardAsync(
                new TokenizeCardRequest(cardNumber, expMMYY, cvv, billingZip), ct);

            if (tokenResult.Success)
            {
                _logger.LogInformation("Card tokenized successfully (not yet attached)");
                return (tokenResult.Value.Token, null);
            }

            _logger.LogError("Card tokenization failed: {Error}", tokenResult.ErrorMessage);
            return (null, tokenResult.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tokenizing card");
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// Attaches a previously tokenized card to a customer and returns the payment method ID.
    /// </summary>
    public async Task<(string? PaymentMethodId, string? ErrorMessage)> AttachTokenAsync(
        string customerId,
        string token,
        string expMMYY,
        bool setAsDefault,
        string? cardholderName = null,
        CancellationToken ct = default)
    {
        try
        {
            var metadata = new Dictionary<string, string>
            {
                ["ExpiryMMYY"] = expMMYY,
                ["TokenAlias"] = "IVR Phone Order"
            };

            if (!string.IsNullOrWhiteSpace(cardholderName))
            {
                var parts = cardholderName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                metadata["BillFirstName"] = parts[0];
                metadata["BillLastName"]  = parts.Length > 1 ? parts[1] : parts[0];
            }

            var attachRequest = new AttachPaymentMethodRequest(
                new CustomerRef(customerId),
                PaymentMethodType.Card,
                token,
                SetAsDefault: setAsDefault,
                IdempotencyKey: Guid.NewGuid().ToString(),
                Metadata.From(metadata)
            );

            var attachResult = await _paymentGatewayService.AttachPaymentMethodAsync(attachRequest, ct);

            if (attachResult.Success)
            {
                _logger.LogInformation("Token attached as payment method: {PaymentMethodId}", attachResult.Value.PaymentMethodId);
                return (attachResult.Value.PaymentMethodId, null);
            }

            _logger.LogError("Failed to attach token: {Error}", attachResult.ErrorMessage);
            return (null, attachResult.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error attaching token");
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// Detaches (removes) a payment method from a customer.
    /// Used when the caller declined to save the card after payment.
    /// </summary>
    public async Task DetachPaymentMethodAsync(string customerId, string paymentMethodId, CancellationToken ct = default)
    {
        try
        {
            var request = new DetachPaymentMethodRequest(
                new CustomerRef(customerId),
                new PaymentMethodRef(paymentMethodId),
                IdempotencyKey: Guid.NewGuid().ToString()
            );

            var result = await _paymentGatewayService.DetachPaymentMethodAsync(request, ct);
            if (result.Success)
                _logger.LogInformation("Payment method detached: {PaymentMethodId}", paymentMethodId);
            else
                _logger.LogWarning("Failed to detach payment method {PaymentMethodId}: {Error}", paymentMethodId, result.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detaching payment method {PaymentMethodId}", paymentMethodId);
        }
    }

    /// <summary>
    /// Processes an IVR donation payment
    /// </summary>
    public async Task<(bool Success, string? PaymentIntentId, string? ErrorMessage)> ProcessPaymentAsync(
        string customerId,
        string paymentMethodId,
        decimal amount,
        string description,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        try
        {
            var request = new CreatePaymentIntentRequest(
                new CustomerRef(customerId),
                new PaymentMethodRef(paymentMethodId),
                Money.Usd(amount),
                description,
                CaptureImmediately: true,
                IdempotencyKey: idempotencyKey,
                Metadata.Empty());

            var result = await _paymentGatewayService.CreatePaymentIntentAsync(request, ct);
            
            if (result.Success)
            {
                _logger.LogInformation("Payment processed successfully: {PaymentIntentId}, Amount: {Amount}", 
                    result.Value.PaymentIntentId, amount);
                return (true, result.Value.PaymentIntentId, null);
            }

            _logger.LogError("Payment processing failed: {Error}", result.ErrorMessage);
            return (false, null, result.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment");
            return (false, null, ex.Message);
        }
    }
}

