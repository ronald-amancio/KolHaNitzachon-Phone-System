using System.ComponentModel.DataAnnotations;

namespace KolHaNitzachon.PhoneSystem.Infrastructure.Payments;

public sealed class CardknoxOptions
{
    public const string SectionName = "Payment:Cardknox";
    [Required] public string BaseUrl { get; init; } = "https://x1.cardknox.com";
    [Required] public string TransactionPath { get; init; } = "/gatewayjson";
    [Required] public string ApiKey { get; init; } = string.Empty;
    [Required] public string ApiVersion { get; init; } = "5.0.0";
    [Required] public string SoftwareName { get; init; } = "KolHaNitzachon.PhoneSystem";
    [Required] public string SoftwareVersion { get; init; } = "1.0.0";
    [Range(5, 120)] public int TimeoutSeconds { get; init; } = 30;
}
