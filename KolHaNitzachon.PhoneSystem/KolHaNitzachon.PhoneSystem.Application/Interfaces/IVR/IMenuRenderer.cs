using KolHaNitzachon.PhoneSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Twilio.TwiML;
using Twilio.TwiML.Voice;

namespace KolHaNitzachon.PhoneSystem.Application.Interfaces.IVR
{
    public interface IMenuRenderer
    {
        VoiceResponse RenderMainMenu(string actionUrl, string recordingBaseUrl);
        VoiceResponse RenderSponsorAllMenu(string actionUrl, string recordingBaseUrl);
        VoiceResponse RenderEnterDonationAmount(string actionUrl, string recordingBaseUrl);
        VoiceResponse RenderInvalidDonationAmount(string actionUrl, string recordingBaseUrl);
        VoiceResponse RenderDonationConfirmation(decimal amount, string actionUrl, string recordingBaseUrl);
        VoiceResponse RenderPaymentSuccessful(string recordingBaseUrl);
        VoiceResponse RenderPaymentFailed(string mainMenuUrl, string recordingBaseUrl);
        VoiceResponse RenderInvalidOption(string actionUrl);
        VoiceResponse RenderPreparingPayment(string recordingBaseUrl);
        //public VoiceResponse RenderPreparingPayment(string recordingBaseUrl)
        //{
        //    var response = new VoiceResponse();

        //    response.Say(
        //        "Your donation amount has been confirmed. " +
        //        "You will now be transferred to the secure " +
        //        "payment step.");

        //    /*
        //     * Do not play charge-successful.mp3 here.
        //     * No payment has been processed yet.
        //     */

        //    response.Hangup();

        //    return response;
        //}
    }
}