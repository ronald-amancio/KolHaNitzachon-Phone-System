using KolHaNitzachon.PhoneSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Twilio.TwiML;

namespace KolHaNitzachon.PhoneSystem.Application.Interfaces.IVR
{
    public interface IMenuRenderer
    {
        VoiceResponse RenderMainMenu(string actionUrl, string recordingBaseUrl);
        VoiceResponse RenderSponsorAllMenu(string actionUrl, string recordingBaseUrl);
        VoiceResponse RenderSponsorSpecificMenu(string actionUrl, string recordingBaseUrl);
        VoiceResponse RenderEnterContestantCode(string actionUrl, string recordingBaseUrl);
        VoiceResponse RenderContestantList(IEnumerable<Recipient> recipients, string actionUrl, string recordingBaseUrl);
        VoiceResponse RenderContestantDonation(Recipient recipient, string actionUrl, string recordingBaseUrl);
        VoiceResponse RenderPledgeConfirmation(Recipient recipient, decimal pledgeAmount, string actionUrl, string recordingBaseUrl);
        VoiceResponse RenderInvalidOption(string actionUrl, string recordingBaseUrl);
        VoiceResponse RenderContestantNotFound(string actionUrl, string recordingBaseUrl);
    }
}