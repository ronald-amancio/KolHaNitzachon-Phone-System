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
        //VoiceResponse RenderMainMenu(string? digits);
        //VoiceResponse RenderSponsorAllMenu(string? digits);
        //VoiceResponse RenderSponsorSpecificMenu(string? digits);
        //VoiceResponse RenderContestantList();
        //VoiceResponse RenderContestantDonation(Recipient recipient);
        //VoiceResponse RenderInvalidOption();

        VoiceResponse RenderMainMenu(string? digits);
    }
}