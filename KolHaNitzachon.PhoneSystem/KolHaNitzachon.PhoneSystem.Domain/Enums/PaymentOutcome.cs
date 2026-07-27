namespace KolHaNitzachon.PhoneSystem.Domain.Payments;

public enum PaymentOutcome
{
    Approved = 1,
    Declined = 2,
    ValidationFailed = 3,
    GatewayTimeout = 4,
    GatewayUnavailable = 5,
    ProcessingFailed = 6,
    DuplicateSubmission = 7
}
