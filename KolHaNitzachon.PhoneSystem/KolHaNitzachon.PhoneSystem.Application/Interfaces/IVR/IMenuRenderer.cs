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
        VoiceResponse RenderEnterCardNumber(string actionUrl);
        VoiceResponse RenderInvalidCardNumber(string actionUrl);
        VoiceResponse RenderEnterExpiryDate(string actionUrl);
        VoiceResponse RenderInvalidExpiryDate(string actionUrl);
        VoiceResponse RenderEnterCvv(string actionUrl);
        VoiceResponse RenderInvalidCvv(string actionUrl);
        VoiceResponse RenderEnterBillingZip(string actionUrl);
        VoiceResponse RenderInvalidBillingZip(string actionUrl);
        VoiceResponse RenderEnterRecipientCode(string actionUrl, string recordingBaseUrl);
        VoiceResponse RenderRecipientNotFound(string actionUrl, string recordingBaseUrl);
        VoiceResponse RenderRecipientChain(Recipient recipient, string donationAmountActionUrl, string recordingBaseUrl);
    }
}