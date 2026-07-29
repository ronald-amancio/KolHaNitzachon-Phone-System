using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KolHaNitzachon.PhoneSystem.Application.Models
{
    public sealed class IvrCallSession
    {
        public required string CallSid { get; init; }

        public string? CallerPhoneNumber { get; set; }

        public DonationType? DonationType { get; set; }

        public Guid? RecipientId { get; set; }

        public int? RecipientCode { get; set; }

        public decimal? DonationAmount { get; set; }

        // Payment Information
        public string? CardNumber { get; set; }

        public string? ExpiryMMYY { get; set; }

        public string? Cvv { get; set; }

        public string? BillingZip { get; set; }


        public string CurrentStep { get; set; } = "main";

        public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;

        public DateTime LastUpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAtUtc { get; set; } =
            DateTime.UtcNow.AddMinutes(30);


       

        public string? CustomerId { get; set; }

        public string? PaymentMethodId { get; set; }

        public string? PaymentIntentId { get; set; }
    }
}