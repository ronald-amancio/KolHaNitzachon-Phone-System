using KolHaNitzachon.PhoneSystem.Application.Interfaces.Voice;
using KolHaNitzachon.PhoneSystem.Infrastructure.SignalWire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace KolHaNitzachon.PhoneSystem.Infrastructure.Services.Voice
{
    public class SignalWireVoiceService : IVoiceService
    {
        private readonly SignalWireSettings _settings;

        public SignalWireVoiceService(IOptions<SignalWireSettings> options)
        {
            _settings = options.Value;

            TwilioClient.Init(
                _settings.ProjectId,
                _settings.Token);

            TwilioClient.SetEdge(_settings.SpaceUrl);
        }

        public async Task<string> CallAsync(
            string destinationNumber,
            string recordingUrl)
        {
            var twimlUrl = $"https://YOUR_PUBLIC_URL/api/twiml?recordingUrl={Uri.EscapeDataString(recordingUrl)}";
            var call = await CallResource.CreateAsync(
                to: new Twilio.Types.PhoneNumber(destinationNumber),
                from: new Twilio.Types.PhoneNumber(_settings.PhoneNumber),
                //url: new Uri(recordingUrl));
                url: new Uri(twimlUrl));

            return call.Sid;
        }
    }
}