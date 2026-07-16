using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KolHaNitzachon.PhoneSystem.Application.Interfaces.Voice
{
    public interface IVoiceService
    {
        Task<string> CallAsync(string destinationNumber, string recordingUrl);
    }
}