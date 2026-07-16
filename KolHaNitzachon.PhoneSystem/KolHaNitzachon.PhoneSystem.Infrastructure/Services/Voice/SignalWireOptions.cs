using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KolHaNitzachon.PhoneSystem.Infrastructure.Services.Voice
{
    public class SignalWireOptions
    {
        public string ProjectId { get; set; } = "";
        public string Token { get; set; } = "";
        public string SpaceUrl { get; set; } = "";
        public string FromNumber { get; set; } = "";
    }
}