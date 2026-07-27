using KolHaNitzachon.PhoneSystem.Domain.Payments;

namespace KolHaNitzachon.PhoneSystem.Application.Payments.Models;

public sealed record PaymentResult
{
    public required PaymentOutcome Outcome { get; init; }
    public string? TransactionReference { get; init; }
    public string? AuthorizationCode { get; init; }
    public string? GatewayStatus { get; init; }
    public string? GatewayErrorCode { get; init; }
    public string? FailureReason { get; init; }
    public string? MaskedCardNumber { get; init; }
    public string? CardType { get; init; }
    public bool IsApproved => Outcome == PaymentOutcome.Approved;

    public static PaymentResult Approved(string transactionReference, string? authorizationCode, string? gatewayStatus, string? maskedCardNumber, string? cardType) => new()
    {
        Outcome = PaymentOutcome.Approved,
        TransactionReference = transactionReference,
        AuthorizationCode = authorizationCode,
        GatewayStatus = gatewayStatus,
        MaskedCardNumber = maskedCardNumber,
        CardType = cardType
    };

    public static PaymentResult Declined(string? transactionReference, string? gatewayStatus, string? gatewayErrorCode, string? failureReason) => new()
    {
        Outcome = PaymentOutcome.Declined,
        TransactionReference = transactionReference,
        GatewayStatus = gatewayStatus,
        GatewayErrorCode = gatewayErrorCode,
        FailureReason = failureReason
    };

    public static PaymentResult Failed(PaymentOutcome outcome, string failureReason, string? gatewayErrorCode = null) => new()
    {
        Outcome = outcome,
        FailureReason = failureReason,
        GatewayErrorCode = gatewayErrorCode
    };
}
