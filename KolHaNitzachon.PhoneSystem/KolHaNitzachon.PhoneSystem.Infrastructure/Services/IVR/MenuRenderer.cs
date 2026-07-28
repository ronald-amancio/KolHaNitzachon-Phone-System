using KolHaNitzachon.PhoneSystem.Application.Interfaces.External;
using KolHaNitzachon.PhoneSystem.Application.Interfaces.IVR;
using KolHaNitzachon.PhoneSystem.Domain.Entities;
using System.Globalization;
using Twilio.TwiML;
using Twilio.TwiML.Voice;

namespace KolHaNitzachon.PhoneSystem.Infrastructure.Services.IVR
{
    public sealed class MenuRenderer : IMenuRenderer
    {
        private readonly INumberAudioComposer _numberAudioComposer;

        public MenuRenderer(INumberAudioComposer numberAudioComposer)
        {
            _numberAudioComposer = numberAudioComposer;
        }

        public VoiceResponse RenderMainMenu(string actionUrl, string recordingBaseUrl)
        {
            var response = new VoiceResponse();

            var gather = new Gather(
                action: CreateAbsoluteUri(actionUrl),
                method: "POST",
                numDigits: 1,
                timeout: 10);

            gather.Play(
                BuildRecordingUri(
                    recordingBaseUrl,
                    RecordingFiles.MainMenu.Part2));

            gather.Play(
                BuildRecordingUri(
                    recordingBaseUrl,
                    RecordingFiles.MainMenu.Part3));

            response.Append(gather);

            // Replays the main menu if the caller enters nothing.
            response.Redirect(
                CreateAbsoluteUri(actionUrl),
                method: "POST");

            return response;
        }

        public VoiceResponse RenderDonationConfirmation(decimal amount, string actionUrl, string recordingBaseUrl)
        {
            var response = new VoiceResponse();

            var gather = new Gather(
                action: new Uri(
                    actionUrl,
                    UriKind.Absolute),
                method: "POST",
                numDigits: 1,
                timeout: 10);

            gather.Say("You entered");

            var wholeAmount = decimal.ToInt32(
                decimal.Truncate(amount));

            if (wholeAmount is >= 1 and <= 1000)
            {
                foreach (var recording in
                         _numberAudioComposer.Compose(
                             wholeAmount))
                {
                    gather.Play(
                        BuildRecordingUri(
                            recordingBaseUrl,
                            $"{RecordingFiles.Numbers.Folder}/{recording}"));
                }
            }
            else
            {
                gather.Say(
                    wholeAmount.ToString(
                        CultureInfo.InvariantCulture));
            }

            gather.Say(
                "dollars. Press 1 to confirm. " +
                "Press 2 to enter a different amount. " +
                "Press 9 to return to the main menu.");

            response.Append(gather);

            response.Redirect(
                new Uri(
                    actionUrl,
                    UriKind.Absolute),
                method: "POST");

            return response;
        }

        public VoiceResponse RenderPreparingPayment(string recordingBaseUrl)
        {
            var response = new VoiceResponse();

            response.Say(
                "Your donation amount has been confirmed. " +
                "You will now proceed to the secure payment step.");

            //    /*
            //     * Do not play charge-successful.mp3 here.
            //     * No payment has been processed yet.
            //     */

            // Temporary until the real payment flow is connected.
            response.Hangup();

            return response;
        }

        public VoiceResponse RenderEnterCardNumber(string actionUrl)
        {
            var response = new VoiceResponse();

            var gather = new Gather(
                action: CreateAbsoluteUri(actionUrl),
                method: "POST",
                timeout: 15,
                finishOnKey: "#");

            gather.Say(
                "Please enter your card number, " +
                "followed by the pound key.");

            response.Append(gather);

            // Repeat the card-number prompt when no digits are entered.
            response.Redirect(
                CreateAbsoluteUri(actionUrl),
                method: "POST");

            return response;
        }

        public VoiceResponse RenderInvalidCardNumber(string actionUrl)
        {
            var response = new VoiceResponse();

            response.Say(
                "The card number entered was not valid. " +
                "Please try again.");

            response.Redirect(
                CreateAbsoluteUri(actionUrl),
                method: "POST");

            return response;
        }

        public VoiceResponse RenderEnterExpiryDate(
    string actionUrl)
        {
            var response = new VoiceResponse();

            var gather = new Gather(
                action: CreateAbsoluteUri(actionUrl),
                method: "POST",
                numDigits: 4,
                timeout: 10);

            gather.Say(
                "Please enter your card expiration date. " +
                "Use two digits for the month and two digits for the year.");

            response.Append(gather);

            response.Redirect(
                CreateAbsoluteUri(actionUrl),
                method: "POST");

            return response;
        }

        public VoiceResponse RenderInvalidExpiryDate(string actionUrl)
        {
            var response = new VoiceResponse();

            response.Say(
                "The expiration date entered was not valid. Please try again.");

            response.Redirect(
                CreateAbsoluteUri(actionUrl),
                method: "POST");

            return response;
        }

        public VoiceResponse RenderSponsorAllMenu(string actionUrl, string recordingBaseUrl)
        {
            var response = new VoiceResponse();

            var gather = new Gather(
                action: CreateAbsoluteUri(actionUrl),
                method: "POST",
                numDigits: 1,
                timeout: 10);

            gather.Play(BuildRecordingUri(recordingBaseUrl, RecordingFiles.SponsorAll.Menu));
            response.Append(gather);
            response.Redirect( CreateAbsoluteUri(actionUrl), method: "POST");

            return response;
        }

        public VoiceResponse RenderEnterDonationAmount(string actionUrl, string recordingBaseUrl)
        {
            var response = new VoiceResponse();

            var gather = new Gather(
                action: CreateAbsoluteUri(actionUrl),
                method: "POST",
                timeout: 12,
                finishOnKey: "#");

            gather.Play(BuildRecordingUri(recordingBaseUrl, RecordingFiles.SponsorAll.EnterAmount));
            response.Append(gather);
            response.Redirect(CreateAbsoluteUri(actionUrl), method: "POST");

            return response;
        }

        public VoiceResponse RenderInvalidDonationAmount(string actionUrl, string recordingBaseUrl)
        {
            var response = new VoiceResponse();
            response.Play(BuildRecordingUri(recordingBaseUrl, RecordingFiles.Payment.MinimumDonation));
            response.Redirect(CreateAbsoluteUri(actionUrl), method: "POST");

            return response;
        }

        public VoiceResponse RenderPaymentSuccessful(string recordingBaseUrl)
        {
            var response = new VoiceResponse();
            response.Play(BuildRecordingUri(recordingBaseUrl, RecordingFiles.Payment.Successful));
            response.Hangup();

            return response;
        }

        public VoiceResponse RenderPaymentFailed(string mainMenuUrl, string recordingBaseUrl)
        {
            var response = new VoiceResponse();
            response.Play(BuildRecordingUri(recordingBaseUrl, RecordingFiles.Payment.NotSuccessful));
            response.Redirect(CreateAbsoluteUri(mainMenuUrl), method: "POST");

            return response;
        }

        public VoiceResponse RenderInvalidOption(string actionUrl)
        {
            var response = new VoiceResponse();

            // No invalid-option recording was included in the new folder,
            // so TTS is used temporarily.
            response.Say("That was not a valid selection. Please try again.");
            response.Redirect(CreateAbsoluteUri(actionUrl), method: "POST");

            return response;
        }

        private static Uri BuildRecordingUri(string recordingBaseUrl, string relativePath)
        {
            var normalizedBaseUrl = recordingBaseUrl.TrimEnd('/');

            var normalizedPath = relativePath
                .Replace("\\", "/")
                .TrimStart('/');

            var encodedSegments = normalizedPath
                .Split(
                    '/',
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.EscapeDataString);

            var encodedPath = string.Join("/", encodedSegments);

            return new Uri(
                $"{normalizedBaseUrl}/{encodedPath}",
                UriKind.Absolute);
        }

        private static Uri CreateAbsoluteUri(string url)
        {
            return new Uri(url, UriKind.Absolute);
        }
    }
}