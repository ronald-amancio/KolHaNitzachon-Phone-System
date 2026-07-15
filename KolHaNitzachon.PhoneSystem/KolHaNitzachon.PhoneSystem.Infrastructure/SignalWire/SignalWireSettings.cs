using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KolHaNitzachon.PhoneSystem.Infrastructure.SignalWire
{
    public class SignalWireSettings
    {
        public string ProjectId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string SpaceUrl { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }
}