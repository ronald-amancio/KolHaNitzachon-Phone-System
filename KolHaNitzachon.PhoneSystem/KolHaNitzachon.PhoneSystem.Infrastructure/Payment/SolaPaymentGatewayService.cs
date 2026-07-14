using KolHaNitzachon.PhoneSystem.Application.Interfaces.Payment;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KolHaNitzachon.PhoneSystem.Infrastructure.Payment;

/// <summary>
/// SOLA Payment Gateway Service implementation using Cardknox API
/// </summary>
public class SolaPaymentGatewayService : IPaymentGatewayService
{
    const string RecurringBaseUrl = "https://api.cardknox.com/v2";
    const string TransactionBaseUrl = "https://x1.cardknox.com/gatewayjson";
    
    static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    readonly HttpClient _http;
    readonly string _apiKey;
    readonly string _softwareName;
    readonly string _softwareVersion;
    readonly string _recurringApiMinorVersion;
    readonly ILogger<SolaPaymentGatewayService> _logger;

    public SolaPaymentGatewayService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<SolaPaymentGatewayService> logger)
    {
        _http = httpClient;
        _logger = logger;
        var section = configuration.GetSection("Sola");
        _apiKey = section["ApiKey"] ?? throw new InvalidOperationException("Sola ApiKey is not configured.");
        _softwareName = section["SoftwareName"] ?? "WixSystem";
        _softwareVersion = section["SoftwareVersion"] ?? "1.0";
        _recurringApiMinorVersion = section["RecurringApiMinorVersion"] ?? "2.1";
    }

    async Task<(T Data, string RawGatewayPayload)> PostRecurringAsync<T>(string path, object body, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{RecurringBaseUrl}{path}");
        req.Headers.TryAddWithoutValidation("Authorization", _apiKey);
        req.Headers.TryAddWithoutValidation("X-Recurring-Api-Version", _recurringApiMinorVersion);
        
        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(body, JsonOpts)) ?? new();
        dict["SoftwareName"] = _softwareName;
        dict["SoftwareVersion"] = _softwareVersion;
        
        req.Content = new StringContent(JsonSerializer.Serialize(dict, JsonOpts), Encoding.UTF8, "application/json");

        using var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();

        var raw = await res.Content.ReadAsStringAsync(ct);
        string payload;
        try
        {
            var inner = JsonSerializer.Deserialize<string>(raw, JsonOpts);
            payload = !string.IsNullOrWhiteSpace(inner) && inner.Contains('{') ? inner : raw;
        }
        catch
        {
            payload = raw;
        }

        return (JsonSerializer.Deserialize<T>(payload, JsonOpts)!, raw);
    }

    async Task<(Dictionary<string, string> Data, string RawGatewayPayload)> PostTransactionAsync(Dictionary<string, string> fields, CancellationToken ct)
    {
        var dict = new Dictionary<string, string>(fields)
        {
            ["xKey"] = _apiKey,
            ["xSoftwareName"] = _softwareName,
            ["xSoftwareVersion"] = _softwareVersion,
            ["xVersion"] = "5.0.0"
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, TransactionBaseUrl);
        req.Content = new StringContent(JsonSerializer.Serialize(dict), Encoding.UTF8, "application/json");

        using var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadAsStringAsync(ct);
        return (JsonSerializer.Deserialize<Dictionary<string, string>>(json)!, json);
    }

    public async Task<GatewayResult<CustomerRef>> UpsertCustomerAsync(UpsertCustomerRequest request, CancellationToken ct = default)
    {
        try
        {
            var resp = await PostRecurringAsync<RecurringCreateCustomerResponse>("/CreateCustomer", new
            {
                CustomerNumber = request.ExternalKey,
                CustomerNotes = request.Metadata.Values.TryGetValue("Notes", out var n) ? n : null,
                Email = request.Email,
                BillFirstName = request.FirstName,
                BillLastName = request.LastName,
                BillPhone = request.Phone
            }, ct);

            if (resp.Data.Result == "S" && !string.IsNullOrWhiteSpace(resp.Data.CustomerId))
                return GatewayResult<CustomerRef>.Ok(new CustomerRef(resp.Data.CustomerId));

            return GatewayResult<CustomerRef>.Fail(resp.Data.ErrorCode ?? "E", resp.Data.Error ?? "Error", resp.RawGatewayPayload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting customer: {ExternalKey}", request.ExternalKey);
            return GatewayResult<CustomerRef>.Fail("E", ex.Message, "");
        }
    }

    public async Task<GatewayResult<PaymentMethodRef>> AttachPaymentMethodAsync(AttachPaymentMethodRequest request, CancellationToken ct = default)
    {
        try
        {
            var tokenType = request.Type == PaymentMethodType.Card ? "CC" : "Check";
            var exp = request.Metadata.Values.TryGetValue("ExpiryMMYY", out var e) ? e : null;
            var tokenAlias = request.Metadata.Values.TryGetValue("TokenAlias", out var a) ? a : null;
            request.Metadata.Values.TryGetValue("BillFirstName", out var billFirst);
            request.Metadata.Values.TryGetValue("BillLastName",  out var billLast);

            var resp = await PostRecurringAsync<RecurringCreatePaymentMethodResponse>("/CreatePaymentMethod", new
            {
                CustomerId = request.Customer.CustomerId,
                Token = request.TokenOrNonce,
                TokenType = tokenType,
                TokenAlias = tokenAlias,
                Exp = exp,
                SetAsDefault = request.SetAsDefault,
                BillFirstName = billFirst,
                BillLastName  = billLast
            }, ct);

            if (resp.Data.Result == "S" && !string.IsNullOrWhiteSpace(resp.Data.PaymentMethodId))
            {
                _logger.LogInformation("Successfully attached payment method: {PaymentMethodId}", resp.Data.PaymentMethodId);
                return GatewayResult<PaymentMethodRef>.Ok(new PaymentMethodRef(resp.Data.PaymentMethodId));
            }

            _logger.LogError("Failed to attach payment method. Result: {Result}, ErrorCode: {ErrorCode}, Error: {Error}, RawResponse: {RawResponse}",
                resp.Data.Result, resp.Data.ErrorCode, resp.Data.Error, resp.RawGatewayPayload);
            return GatewayResult<PaymentMethodRef>.Fail(resp.Data.ErrorCode ?? "E", resp.Data.Error ?? "Error", resp.RawGatewayPayload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception attaching payment method for customer: {CustomerId}", request.Customer.CustomerId);
            return GatewayResult<PaymentMethodRef>.Fail("E", ex.Message, "");
        }
    }

    public async Task<GatewayResult<Unit>> SetDefaultPaymentMethodAsync(SetDefaultPaymentMethodRequest request, bool setAsDefault = true, CancellationToken ct = default)
    {
        try
        {
            var get = await PostRecurringAsync<RecurringGetPaymentMethodResponse>("/GetPaymentMethod", new
            {
                PaymentMethodId = request.PaymentMethod.PaymentMethodId
            }, ct);

            if (get.Data.Result != "S")
                return GatewayResult<Unit>.Fail(get.Data.ErrorCode ?? "E", get.Data.Error ?? "Error", get.RawGatewayPayload);

            var upd = await PostRecurringAsync<RecurringBaseResponse>("/UpdatePaymentMethod", new
            {
                PaymentMethodId = request.PaymentMethod.PaymentMethodId,
                Revision = get.Data.Revision,
                TokenAlias = get.Data.TokenAlias,
                Exp = get.Data.Exp,
                Street = get.Data.Street,
                Zip = get.Data.Zip,
                SetAsDefault = true
            }, ct);

            return upd.Data.Result == "S"
                ? GatewayResult<Unit>.Ok(new Unit())
                : GatewayResult<Unit>.Fail(upd.Data.ErrorCode ?? "E", upd.Data.Error ?? "Error", upd.RawGatewayPayload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default payment method: {PaymentMethodId}", request.PaymentMethod.PaymentMethodId);
            return GatewayResult<Unit>.Fail("E", ex.Message, "");
        }
    }

    public async Task<GatewayResult<Unit>> DetachPaymentMethodAsync(DetachPaymentMethodRequest request, CancellationToken ct = default)
    {
        try
        {
            var resp = await PostRecurringAsync<RecurringBaseResponse>("/DeletePaymentMethod", new
            {
                PaymentMethodId = request.PaymentMethod.PaymentMethodId
            }, ct);

            return resp.Data.Result == "S"
                ? GatewayResult<Unit>.Ok(new Unit())
                : GatewayResult<Unit>.Fail(resp.Data.ErrorCode ?? "E", resp.Data.Error ?? "Error", resp.RawGatewayPayload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detaching payment method: {PaymentMethodId}", request.PaymentMethod.PaymentMethodId);
            return GatewayResult<Unit>.Fail("E", ex.Message, "");
        }
    }

    public async Task<GatewayResult<IReadOnlyList<PaymentMethodView>>> ListPaymentMethodsAsync(ListPaymentMethodsRequest request, CancellationToken ct = default)
    {
        try
        {
            var resp = await PostRecurringAsync<RecurringListPaymentMethodsResponse>("/ListPaymentMethods", new
            {
                Filters = new { CustomerId = request.Customer.CustomerId }
            }, ct);

            if (resp.Data.Result != "S" || resp.Data.PaymentMethods == null)
                return GatewayResult<IReadOnlyList<PaymentMethodView>>.Fail(resp.Data.ErrorCode ?? "E", resp.Data.Error ?? "Error", resp.RawGatewayPayload);

            var list = resp.Data.PaymentMethods.Select(pm =>
            {
                var type = pm.TokenType == "CC" ? PaymentMethodType.Card : PaymentMethodType.Ach;
                var last4 = pm.MaskedNumber?.Length >= 4 ? pm.MaskedNumber[^4..] : pm.MaskedNumber ?? "";
                var brand = pm.CardType ?? "";
                var expiry = ParseExpiry(pm.Exp);

                return new PaymentMethodView(new PaymentMethodRef(pm.PaymentMethodId), type, pm.IsDefaultPaymentMethod, last4, brand, expiry);
            }).ToList().AsReadOnly();

            return GatewayResult<IReadOnlyList<PaymentMethodView>>.Ok(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing payment methods for customer: {CustomerId}", request.Customer.CustomerId);
            return GatewayResult<IReadOnlyList<PaymentMethodView>>.Fail("E", ex.Message, "");
        }
    }

    public async Task<GatewayResult<PaymentIntentRef>> CreatePaymentIntentAsync(CreatePaymentIntentRequest request, CancellationToken ct = default)
    {
        try
        {
            var txnType = request.CaptureImmediately ? "Sale" : "AuthOnly";

            var resp = await PostRecurringAsync<RecurringProcessTransactionResponse>("/ProcessTransaction", new
            {
                PaymentMethodId = request.PaymentMethod.PaymentMethodId,
                Amount = request.Amount.Amount,
                TransactionType = txnType,
                Description = request.Description,
                AllowDuplicates = false
            }, ct);

            if (resp.Data.Result == "S" && !string.IsNullOrWhiteSpace(resp.Data.GatewayRefNum))
            {
                _logger.LogInformation("Payment processed successfully: {PaymentIntentId}, Amount: {Amount}", resp.Data.GatewayRefNum, request.Amount.Amount);
                return GatewayResult<PaymentIntentRef>.Ok(new PaymentIntentRef(resp.Data.GatewayRefNum));
            }

            _logger.LogError("Payment processing failed. Result: {Result}, ErrorCode: {ErrorCode}, Error: {Error}, RawResponse: {RawResponse}",
                resp.Data.Result, resp.Data.ErrorCode, resp.Data.Error, resp.RawGatewayPayload);
            return GatewayResult<PaymentIntentRef>.Fail(resp.Data.ErrorCode ?? "E", resp.Data.Error ?? "Error", resp.RawGatewayPayload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception creating payment intent: {PaymentMethodId}, Amount: {Amount}", request.PaymentMethod.PaymentMethodId, request.Amount.Amount);
            return GatewayResult<PaymentIntentRef>.Fail("E", ex.Message, "");
        }
    }

    public async Task<GatewayResult<CaptureResult>> CaptureAsync(CaptureRequest request, CancellationToken ct = default)
    {
        try
        {
            var res = await PostTransactionAsync(new Dictionary<string, string>
            {
                ["xCommand"] = "cc:capture",
                ["xRefNum"] = request.PaymentIntent.PaymentIntentId,
                ["xAmount"] = request.AmountToCapture.Amount.ToString("0.00")
            }, ct);

            var ok = res.Data.TryGetValue("xResult", out var r) && r.Equals("A", StringComparison.OrdinalIgnoreCase);

            if (ok)
                return GatewayResult<CaptureResult>.Ok(new CaptureResult(
                    new CaptureRef(res.Data.GetValueOrDefault("xRefNum") ?? request.PaymentIntent.PaymentIntentId),
                    PaymentStatus.Succeeded,
                    res.Data.GetValueOrDefault("xResult", ""),
                    res.Data.GetValueOrDefault("xError", ""),
                    DateTimeOffset.UtcNow));

            return GatewayResult<CaptureResult>.Fail(
                res.Data.GetValueOrDefault("xErrorCode", "E"),
                res.Data.GetValueOrDefault("xError", "Error"),
                res.RawGatewayPayload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing payment: {PaymentIntentId}", request.PaymentIntent.PaymentIntentId);
            return GatewayResult<CaptureResult>.Fail("E", ex.Message, "");
        }
    }

    public async Task<GatewayResult<VoidResult>> VoidAsync(VoidRequest request, CancellationToken ct = default)
    {
        try
        {
            var res = await PostTransactionAsync(new Dictionary<string, string>
            {
                ["xCommand"] = "cc:void",
                ["xRefNum"] = request.PaymentIntent.PaymentIntentId
            }, ct);

            var ok = res.Data.TryGetValue("xResult", out var r) && r.Equals("A", StringComparison.OrdinalIgnoreCase);

            if (ok)
                return GatewayResult<VoidResult>.Ok(new VoidResult(
                    PaymentStatus.Voided,
                    res.Data.GetValueOrDefault("xResult", ""),
                    res.Data.GetValueOrDefault("xError", ""),
                    DateTimeOffset.UtcNow));

            return GatewayResult<VoidResult>.Fail(
                res.Data.GetValueOrDefault("xErrorCode", "E"),
                res.Data.GetValueOrDefault("xError", "Error"),
                res.RawGatewayPayload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error voiding payment: {PaymentIntentId}", request.PaymentIntent.PaymentIntentId);
            return GatewayResult<VoidResult>.Fail("E", ex.Message, "");
        }
    }

    public async Task<GatewayResult<TokenRef>> TokenizeCardAsync(TokenizeCardRequest request, CancellationToken ct = default)
    {
        try
        {
            var (data, raw) = await PostTransactionAsync(new Dictionary<string, string>
            {
                ["xCommand"] = "cc:save",
                ["xCardNum"] = request.CardNumber,
                ["xExp"] = request.ExpMMYY,
                ["xCVV"] = request.Cvv,
                ["xBillZip"] = request.BillingZip
            }, ct);

            var ok = data.TryGetValue("xResult", out var r) && r.Equals("A", StringComparison.OrdinalIgnoreCase);
            if (ok && data.TryGetValue("xToken", out var token) && !string.IsNullOrWhiteSpace(token))
            {
                _logger.LogInformation("Card tokenized successfully");
                return GatewayResult<TokenRef>.Ok(new TokenRef(token));
            }

            _logger.LogError("Card tokenization failed. Result: {Result}, ErrorCode: {ErrorCode}, Error: {Error}",
                r, data.GetValueOrDefault("xErrorCode"), data.GetValueOrDefault("xError"));
            return GatewayResult<TokenRef>.Fail(
                data.GetValueOrDefault("xErrorCode", "E"),
                data.GetValueOrDefault("xError", "Card tokenization failed"),
                raw);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tokenizing card");
            return GatewayResult<TokenRef>.Fail("E", ex.Message, "");
        }
    }

    private static DateOnly? ParseExpiry(string? exp)
    {
        if (string.IsNullOrWhiteSpace(exp) || exp.Length < 4)
            return null;

        if (int.TryParse(exp.Substring(0, 2), out var month) && int.TryParse(exp.Substring(2, 2), out var year))
        {
            var fullYear = year < 50 ? 2000 + year : 1900 + year;
            if (month >= 1 && month <= 12)
                return new DateOnly(fullYear, month, 1);
        }

        return null;
    }

    // Response models for Cardknox API
    private class RecurringBaseResponse
    {
        public string Result { get; set; } = "";
        public string? ErrorCode { get; set; }
        public string? Error { get; set; }
    }

    private class RecurringCreateCustomerResponse : RecurringBaseResponse
    {
        public string? CustomerId { get; set; }
    }

    private class RecurringCreatePaymentMethodResponse : RecurringBaseResponse
    {
        public string? PaymentMethodId { get; set; }
    }

    private class RecurringGetPaymentMethodResponse : RecurringBaseResponse
    {
        public int Revision { get; set; }
        public string? TokenAlias { get; set; }
        public string? Exp { get; set; }
        public string? Street { get; set; }
        public string? Zip { get; set; }
    }

    private class RecurringListPaymentMethodsResponse : RecurringBaseResponse
    {
        public List<PaymentMethodDto>? PaymentMethods { get; set; }
    }

    private class PaymentMethodDto
    {
        public string PaymentMethodId { get; set; } = "";
        public string? TokenType { get; set; }
        public string? MaskedNumber { get; set; }
        public string? CardType { get; set; }
        public string? Exp { get; set; }
        public bool IsDefaultPaymentMethod { get; set; }
    }

    private class RecurringProcessTransactionResponse : RecurringBaseResponse
    {
        public string? GatewayRefNum { get; set; }
    }
}

