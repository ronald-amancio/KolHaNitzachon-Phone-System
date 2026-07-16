using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KolHaNitzachon.PhoneSystem.Application.DTOs.Recipient
{
    public class RecipientResponse
    {
        public Guid Id { get; set; }
        public int Code { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NameRecordingUrl { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}