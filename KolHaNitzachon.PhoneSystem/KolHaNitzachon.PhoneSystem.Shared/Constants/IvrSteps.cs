using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KolHaNitzachon.PhoneSystem.Shared.Constants
{
    public static class IvrSteps
    {
        // Main Flow
        public const string Main = "main";
        public const string RecipientSelection = "recipient-selection";
        public const string DonationAmount = "donation-amount";
        public const string ConfirmDonation = "confirm-donation";
        public const string SponsorAll = "sponsor-all";

        // Payment Flow
        public const string PaymentCardNumber = "payment-card-number";
        public const string PaymentExpiry = "payment-expiry";
        public const string PaymentCvv = "payment-cvv";
        public const string PaymentZip = "payment-zip";
        public const string PaymentTokenize = "payment-tokenize";
        public const string PaymentProcess = "payment-process";

        // Result
        public const string PaymentSuccess = "payment-success";
        public const string PaymentFailure = "payment-failure";

        // Misc
        public const string EndCall = "end-call";
    }
}