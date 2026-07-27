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

        public string CurrentStep { get; set; } = "main";

        public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;

        public DateTime LastUpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAtUtc { get; set; } =
            DateTime.UtcNow.AddMinutes(30);
    }
}