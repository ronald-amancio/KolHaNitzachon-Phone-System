using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KolHaNitzachon.PhoneSystem.Application.Payments.Interfaces;
using KolHaNitzachon.PhoneSystem.Application.Payments.Models;
using KolHaNitzachon.PhoneSystem.Domain.Payments;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KolHaNitzachon.PhoneSystem.Infrastructure.Payments;

public sealed class CardknoxPaymentGateway : IPaymentGateway
{
    private readonly HttpClient _httpClient;
    private readonly CardknoxOptions _options;
    private readonly IPaymentRequestValidator _validator;
    private readonly ILogger<CardknoxPaymentGateway> _logger;

    public CardknoxPaymentGateway(HttpClient httpClient, IOptions<CardknoxOptions> options, IPaymentRequestValidator validator, ILogger<CardknoxPaymentGateway> logger)
    {
        _httpClient = httpClient; _options = options.Value; _validator = validator; _logger = logger;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        var validation = _validator.Validate(request);
        if (!validation.IsValid) return PaymentResult.Failed(PaymentOutcome.ValidationFailed, string.Join(" ", validation.Errors));

        var gatewayRequest = new CardknoxGatewayRequest
        {
            Key = _options.ApiKey, Version = _options.ApiVersion, SoftwareName = _options.SoftwareName, SoftwareVersion = _options.SoftwareVersion,
            Amount = request.Amount.ToString("0.00", CultureInfo.InvariantCulture), Currency = request.Currency.ToUpperInvariant(),
            InvoiceNumber = request.InvoiceNumber, PaymentToken = request.PaymentToken, CvvToken = Clean(request.CvvToken),
            CardholderName = Clean(request.CardholderName), BillingZipCode = Clean(request.BillingZipCode), Description = Clean(request.Description),
            IdempotencyKey = request.IdempotencyKey, CallSid = Clean(request.CallSid), RecipientId = request.RecipientId?.ToString()
        };

        try
        {
            _logger.LogInformation("Submitting payment. Invoice={Invoice}, Amount={Amount}, CallSid={CallSid}, IdempotencyKey={IdempotencyKey}", request.InvoiceNumber, request.Amount, request.CallSid, request.IdempotencyKey);
            using var response = await _httpClient.PostAsJsonAsync(_options.TransactionPath, gatewayRequest, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return response.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout
                    ? PaymentResult.Failed(PaymentOutcome.GatewayTimeout, "The payment gateway timed out.")
                    : PaymentResult.Failed(PaymentOutcome.GatewayUnavailable, "The payment gateway is temporarily unavailable.");
            }
            return Parse(body);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return PaymentResult.Failed(PaymentOutcome.GatewayTimeout, "The payment gateway did not respond in time.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Unable to reach Cardknox for invoice {Invoice}", request.InvoiceNumber);
            return PaymentResult.Failed(PaymentOutcome.GatewayUnavailable, "The payment gateway is temporarily unavailable.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Unreadable Cardknox response for invoice {Invoice}", request.InvoiceNumber);
            return PaymentResult.Failed(PaymentOutcome.ProcessingFailed, "The payment response could not be processed.");
        }
    }

    private static PaymentResult Parse(string body)
    {
        using var doc = JsonDocument.Parse(body); var root = doc.RootElement;
        var result = Read(root, "xResult"); var status = Read(root, "xStatus"); var errorCode = Read(root, "xErrorCode"); var error = Read(root, "xError");
        var reference = Read(root, "xRefNum") ?? Read(root, "xRefnum"); var auth = Read(root, "xAuthCode"); var masked = Read(root, "xMaskedCardNumber"); var cardType = Read(root, "xCardType");
        var approved = string.Equals(result, "A", StringComparison.OrdinalIgnoreCase) || string.Equals(status, "Approved", StringComparison.OrdinalIgnoreCase);
        if (approved) return string.IsNullOrWhiteSpace(reference) ? PaymentResult.Failed(PaymentOutcome.ProcessingFailed, "Approved response had no transaction reference.") : PaymentResult.Approved(reference, auth, status ?? "Approved", masked, cardType);
        var declined = string.Equals(result, "D", StringComparison.OrdinalIgnoreCase) || (!string.IsNullOrWhiteSpace(status) && status.Contains("declin", StringComparison.OrdinalIgnoreCase));
        if (declined) return PaymentResult.Declined(reference, status ?? "Declined", errorCode, Safe(error));
        return PaymentResult.Failed(PaymentOutcome.ProcessingFailed, Safe(error) ?? "The gateway could not process the transaction.", errorCode);
    }

    private static string? Read(JsonElement root, string name)
    {
        foreach (var p in root.EnumerateObject()) if (p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString() : p.Value.GetRawText();
        return null;
    }
    private static string? Safe(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Length <= 300 ? value : value[..300];
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
