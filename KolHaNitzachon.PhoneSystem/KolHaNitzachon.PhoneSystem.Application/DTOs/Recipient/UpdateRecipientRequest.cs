using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KolHaNitzachon.PhoneSystem.Application.DTOs.Recipient
{
    public class UpdateRecipientRequest
    {
        //public Guid Id { get; set; }
        //public int Code { get; set; }
        //public string Name { get; set; } = string.Empty;
        //public DateTime StartDate { get; set; }
        //public DateTime? EndDate { get; set; }

        public int Code { get; set; }
        public string Name { get; set; }
        public string NameRecordingUrl { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}