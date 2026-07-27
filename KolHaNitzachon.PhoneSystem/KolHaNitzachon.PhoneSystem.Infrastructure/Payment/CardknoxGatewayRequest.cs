using System.Text.Json.Serialization;

namespace KolHaNitzachon.PhoneSystem.Infrastructure.Payments;

internal sealed record CardknoxGatewayRequest
{
    [JsonPropertyName("xKey")] public required string Key { get; init; }
    [JsonPropertyName("xVersion")] public required string Version { get; init; }
    [JsonPropertyName("xSoftwareName")] public required string SoftwareName { get; init; }
    [JsonPropertyName("xSoftwareVersion")] public required string SoftwareVersion { get; init; }
    [JsonPropertyName("xCommand")] public string Command { get; init; } = "cc:sale";
    [JsonPropertyName("xAmount")] public required string Amount { get; init; }
    [JsonPropertyName("xCurrency")] public required string Currency { get; init; }
    [JsonPropertyName("xInvoice")] public required string InvoiceNumber { get; init; }
    [JsonPropertyName("xCardNum")] public required string PaymentToken { get; init; }
    [JsonPropertyName("xCVV"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? CvvToken { get; init; }
    [JsonPropertyName("xName"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? CardholderName { get; init; }
    [JsonPropertyName("xBillZip"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? BillingZipCode { get; init; }
    [JsonPropertyName("xDescription"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? Description { get; init; }
    [JsonPropertyName("xCustom01")] public required string IdempotencyKey { get; init; }
    [JsonPropertyName("xCustom02"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? CallSid { get; init; }
    [JsonPropertyName("xCustom03"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? RecipientId { get; init; }
}
