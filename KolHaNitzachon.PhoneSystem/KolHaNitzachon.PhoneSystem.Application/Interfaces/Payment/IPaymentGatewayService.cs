namespace KolHaNitzachon.PhoneSystem.Application.Interfaces.Payment;

/// <summary>
/// Payment gateway service interface for SOLA/Cardknox integration
/// </summary>
public interface IPaymentGatewayService
{
    Task<GatewayResult<CustomerRef>> UpsertCustomerAsync(UpsertCustomerRequest request, CancellationToken ct = default);
    Task<GatewayResult<PaymentMethodRef>> AttachPaymentMethodAsync(AttachPaymentMethodRequest request, CancellationToken ct = default);
    Task<GatewayResult<Unit>> SetDefaultPaymentMethodAsync(SetDefaultPaymentMethodRequest request, bool setAsDefault = true, CancellationToken ct = default);
    Task<GatewayResult<Unit>> DetachPaymentMethodAsync(DetachPaymentMethodRequest request, CancellationToken ct = default);
    Task<GatewayResult<IReadOnlyList<PaymentMethodView>>> ListPaymentMethodsAsync(ListPaymentMethodsRequest request, CancellationToken ct = default);
    Task<GatewayResult<PaymentIntentRef>> CreatePaymentIntentAsync(CreatePaymentIntentRequest request, CancellationToken ct = default);
    Task<GatewayResult<CaptureResult>> CaptureAsync(CaptureRequest request, CancellationToken ct = default);
    Task<GatewayResult<VoidResult>> VoidAsync(VoidRequest request, CancellationToken ct = default);
    Task<GatewayResult<TokenRef>> TokenizeCardAsync(TokenizeCardRequest request, CancellationToken ct = default);
}

public sealed record GatewayResult<T>(bool Success, T Value, string ErrorCode, string ErrorMessage, string RawGatewayPayload, string ProviderTraceId)
{
    public static GatewayResult<T> Ok(T value, string providerTraceId = "") => new(true, value, string.Empty, string.Empty, string.Empty, providerTraceId);
    public static GatewayResult<T> Fail(string code, string message, string rawGatewayPayload, string providerTraceId = "") => new(false, default!, code, message, rawGatewayPayload, providerTraceId);
}

public sealed record Unit();

public enum CurrencyCode { USD = 840 }

public enum PaymentMethodType { Card, Ach }

public enum PaymentStatus { Succeeded, Pending, Failed, Authorized, Voided, Refunded, PartiallyRefunded }

public sealed record Money(decimal Amount, CurrencyCode Currency)
{
    public static Money Usd(decimal amount) => new(amount, CurrencyCode.USD);
}

public sealed record CustomerRef(string CustomerId);

public sealed record PaymentMethodRef(string PaymentMethodId);

public sealed record TokenRef(string Token);

public sealed record PaymentIntentRef(string PaymentIntentId);

public sealed record CaptureRef(string CaptureId);

public sealed record RefundRef(string RefundId);

public sealed record Metadata(IDictionary<string, string> Values)
{
    public static Metadata Empty() => new(new Dictionary<string, string>());
    public static Metadata From(IDictionary<string, string> values) => new(values);
}

public sealed record UpsertCustomerRequest(string ExternalKey, string Email, string FirstName, string LastName, string Phone, string IdempotencyKey, Metadata Metadata);

public sealed record AttachPaymentMethodRequest(CustomerRef Customer, PaymentMethodType Type, string TokenOrNonce, bool SetAsDefault, string IdempotencyKey, Metadata Metadata);

public sealed record SetDefaultPaymentMethodRequest(CustomerRef Customer, PaymentMethodRef PaymentMethod, string IdempotencyKey);

public sealed record DetachPaymentMethodRequest(CustomerRef Customer, PaymentMethodRef PaymentMethod, string IdempotencyKey);

public sealed record ListPaymentMethodsRequest(CustomerRef Customer);

public sealed record PaymentMethodView(PaymentMethodRef PaymentMethod, PaymentMethodType Type, bool IsDefault, string Last4, string Brand, DateOnly? Expiry);

public sealed record CreatePaymentTokenRequest(PaymentMethodType Type, string RawPanOrBankData, string Cvv, int ExpMonth, int ExpYear, string BillingPostalCode);

public sealed record TokenizeCardRequest(string CardNumber, string ExpMMYY, string Cvv, string BillingZip);

public sealed record CreatePaymentIntentRequest(CustomerRef Customer, PaymentMethodRef PaymentMethod, Money Amount, string Description, bool CaptureImmediately, string IdempotencyKey, Metadata Metadata);

public sealed record CaptureRequest(PaymentIntentRef PaymentIntent, Money AmountToCapture, string IdempotencyKey);

public sealed record CaptureResult(CaptureRef Capture, PaymentStatus Status, string ProcessorCode, string ProcessorMessage, DateTimeOffset CapturedAtUtc);

public sealed record VoidRequest(PaymentIntentRef PaymentIntent, string IdempotencyKey);

public sealed record VoidResult(PaymentStatus Status, string ProcessorCode, string ProcessorMessage, DateTimeOffset VoidedAtUtc);

public sealed record RefundRequest(PaymentIntentRef PaymentIntent, Money AmountToRefund, string Reason, string IdempotencyKey);

public sealed record RefundResult(RefundRef Refund, PaymentStatus Status, string ProcessorCode, string ProcessorMessage, DateTimeOffset RefundedAtUtc);

public sealed record HandleWebhookRequest(string Provider, string RawPayload, string SignatureHeader, DateTimeOffset ReceivedAtUtc);

public sealed record WebhookHandlingResult(string EventType, string RelatedId, PaymentStatus Status, Money? Amount, DateTimeOffset OccurredAtUtc);



