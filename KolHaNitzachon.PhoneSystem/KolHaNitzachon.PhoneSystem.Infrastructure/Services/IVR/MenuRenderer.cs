using KolHaNitzachon.PhoneSystem.Application.Interfaces.IVR;
using KolHaNitzachon.PhoneSystem.Application.Interfaces.Recordings;
using KolHaNitzachon.PhoneSystem.Domain.Entities;
using System.Globalization;
using Twilio.TwiML;
using Twilio.TwiML.Voice;

namespace KolHaNitzachon.PhoneSystem.Infrastructure.Services.IVR
{
    public sealed class MenuRenderer : IMenuRenderer
    {
        private readonly INumberAudioComposer _numberAudioComposer;
        private readonly IAudioPromptUrlProvider _audioPromptUrlProvider;

        public MenuRenderer(INumberAudioComposer numberAudioComposer, IAudioPromptUrlProvider audioPromptUrlProvider)
        {
            _numberAudioComposer = numberAudioComposer;
            _audioPromptUrlProvider = audioPromptUrlProvider;
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

        public VoiceResponse RenderEnterCvv(string actionUrl)
        {
            var response = new VoiceResponse();

            var gather = new Gather(
                action: CreateAbsoluteUri(actionUrl),
                method: "POST",
                timeout: 10,
                finishOnKey: "#");

            gather.Say(
                "Please enter the three or four digit " +
                "security code from your card, " +
                "followed by the pound key.");

            response.Append(gather);

            response.Redirect(
                CreateAbsoluteUri(actionUrl),
                method: "POST");

            return response;
        }

        public VoiceResponse RenderEnterBillingZip(string actionUrl)
        {
            var response = new VoiceResponse();

            var gather = new Gather(
                action: new Uri(actionUrl),
                method: Twilio.Http.HttpMethod.Post,
                input: new List<Gather.InputEnum>
                {
            Gather.InputEnum.Dtmf
                },
                numDigits: 5,
                timeout: 10,
                finishOnKey: "#");

            gather.Say(
                "Please enter the five digit billing ZIP code associated with your card.");

            response.Append(gather);

            response.Redirect(
                new Uri(actionUrl),
                method: Twilio.Http.HttpMethod.Post);

            return response;
        }

        public VoiceResponse RenderInvalidBillingZip(string actionUrl)
        {
            var response = new VoiceResponse();

            response.Say(
                "The billing ZIP code you entered is invalid.");

            var gather = new Gather(
                action: new Uri(actionUrl),
                method: Twilio.Http.HttpMethod.Post,
                input: new List<Gather.InputEnum>
                {
            Gather.InputEnum.Dtmf
                },
                numDigits: 5,
                timeout: 10,
                finishOnKey: "#");

            gather.Say(
                "Please enter the five digit billing ZIP code associated with your card.");

            response.Append(gather);

            response.Redirect(
                new Uri(actionUrl),
                method: Twilio.Http.HttpMethod.Post);

            return response;
        }

        public VoiceResponse RenderInvalidCvv(string actionUrl)
        {
            var response = new VoiceResponse();

            response.Say(
                "The security code entered was not valid. " +
                "Please try again.");

            response.Redirect(
                CreateAbsoluteUri(actionUrl),
                method: "POST");

            return response;
        }

        public VoiceResponse RenderEnterRecipientCode(string actionUrl, string recordingBaseUrl)
        {
            var response = new VoiceResponse();
            var gather = new Gather(
                action: CreateAbsoluteUri(actionUrl),
                method: "POST",
                timeout: 12,
                finishOnKey: "#");

            gather.Play(BuildRecordingUri(recordingBaseUrl, RecordingFiles.Recipient.EnterCode));
            response.Append(gather);
            response.Redirect(CreateAbsoluteUri(actionUrl), method: "POST");
            return response;
        }

        #region RenderRecipientNotFound temporary implementation commented if the prompt was already in placed
        //public VoiceResponse RenderRecipientNotFound(string actionUrl, string recordingBaseUrl)
        //{
        //    var response = new VoiceResponse();
        //    response.Play(BuildRecordingUri(recordingBaseUrl, RecordingFiles.Recipient.NotFound));
        //    response.Redirect(CreateAbsoluteUri(actionUrl), method: "POST");
        //    return response;
        //}
        #endregion

        public VoiceResponse RenderRecipientNotFound(string actionUrl, string recordingBaseUrl)
        {
            var response = new VoiceResponse();

            response.Say(
                "The recipient code entered was not found " +
                "or is not currently active. Please try again.");

            response.Redirect(
                new Uri(
                    actionUrl,
                    UriKind.Absolute),
                method: "POST");

            return response;
        }

        public VoiceResponse RenderRecipientChain(Recipient recipient, string donationAmountActionUrl, string recordingBaseUrl)
        {
            ArgumentNullException.ThrowIfNull(recipient);

            var response = new VoiceResponse();
            var gather = new Gather(
                action: CreateAbsoluteUri(donationAmountActionUrl),
                method: "POST",
                timeout: 12,
                finishOnKey: "#");

            gather.Play(BuildRecordingUri(recordingBaseUrl, RecordingFiles.Recipient.RecipientPrefix));

            if (!string.IsNullOrWhiteSpace(recipient.NameRecordingUrl))
            {
                var nameUri = Uri.TryCreate(recipient.NameRecordingUrl, UriKind.Absolute, out var absoluteNameUri)
                    ? absoluteNameUri
                    : BuildRecordingUri(recordingBaseUrl, recipient.NameRecordingUrl);
                gather.Play(nameUri);
            }
            else
            {
                gather.Say(recipient.Name);
            }

            gather.Play(BuildRecordingUri(recordingBaseUrl, RecordingFiles.Recipient.HasBeenCompetingFor));

            var endDate = recipient.EndDate?.Date ?? DateTime.UtcNow.Date;
            var days = Math.Clamp((endDate - recipient.StartDate.Date).Days + 1, 0, 1000);
            foreach (var recording in _numberAudioComposer.Compose(days))
            {
                gather.Play(BuildRecordingUri(recordingBaseUrl, $"{RecordingFiles.Numbers.Folder}/{recording}"));
            }

            gather.Play(BuildRecordingUri(recordingBaseUrl, RecordingFiles.Recipient.Days));
            gather.Play(BuildRecordingUri(recordingBaseUrl, RecordingFiles.Recipient.CodeNumberToSponsor));

            if (recipient.Code is >= 0 and <= 1000)
            {
                foreach (var recording in _numberAudioComposer.Compose(recipient.Code))
                {
                    gather.Play(BuildRecordingUri(recordingBaseUrl, $"{RecordingFiles.Numbers.Folder}/{recording}"));
                }
            }
            else
            {
                gather.Say(recipient.Code.ToString(CultureInfo.InvariantCulture));
            }

            gather.Play(BuildRecordingUri(recordingBaseUrl, RecordingFiles.Recipient.EnterPledgeAmount));
            response.Append(gather);
            response.Redirect(CreateAbsoluteUri(donationAmountActionUrl), method: "POST");
            return response;
        }

        public VoiceResponse RenderActiveContestants(
    IReadOnlyCollection<Recipient> recipients,
    DateTime businessToday,
    string recipientSelectionActionUrl,
    string recordingBaseUrl)
        {
            ArgumentNullException.ThrowIfNull(recipients);

            var response = new VoiceResponse();

            var gather = new Gather(
                action: CreateAbsoluteUri(recipientSelectionActionUrl),
                method: "POST",
                timeout: 12,
                finishOnKey: "#");

            if (recipients.Count == 0)
            {
                gather.Say(
                    "There are currently no active contestants. " +
                    "Press star to return to the main menu.");
            }
            else
            {
                gather.Say(
                    "The following is the full list of active contestants " +
                    "currently competing in the championship.");

                foreach (var recipient in recipients)
                {
                    gather.Say("Contestant");

                    PlayRecipientName(
                        gather,
                        recipient,
                        recordingBaseUrl);

                    gather.Play(
                        BuildRecordingUri(
                            recordingBaseUrl,
                            RecordingFiles.Recipient.HasBeenCompetingFor));

                    var daysCompeting = Math.Max(
                        1,
                        (businessToday.Date -
                         recipient.StartDate.Date).Days + 1);

                    PlayNumber(
                        gather,
                        daysCompeting,
                        recordingBaseUrl);

                    gather.Play(
                        BuildRecordingUri(
                            recordingBaseUrl,
                            RecordingFiles.Recipient.Days));

                    gather.Play(
                        BuildRecordingUri(
                            recordingBaseUrl,
                            RecordingFiles.Recipient.CodeNumberToSponsor));

                    PlayNumber(
                        gather,
                        recipient.Code,
                        recordingBaseUrl);
                }

                gather.Say(
                    "To sponsor a contestant, please enter their code number " +
                    "and press the pound key. " +
                    "Press star to return to the main menu.");
            }

            response.Append(gather);

            response.Redirect(
                CreateAbsoluteUri(recipientSelectionActionUrl),
                method: "POST");

            return response;
        }


        #region Helpers
        private void PlayRecipientName(Gather gather, Recipient recipient, string recordingBaseUrl)
        {
            if (!string.IsNullOrWhiteSpace(recipient.NameRecordingUrl))
            {
                var nameUri =
                    Uri.TryCreate(
                        recipient.NameRecordingUrl,
                        UriKind.Absolute,
                        out var absoluteNameUri)
                        ? absoluteNameUri
                        : BuildRecordingUri(
                            recordingBaseUrl,
                            recipient.NameRecordingUrl);

                gather.Play(nameUri);
                return;
            }

            gather.Say(recipient.Name);
        }

        private void PlayNumber(
            Gather gather,
            int number,
            string recordingBaseUrl)
        {
            if (number is >= 0 and <= 999_999)
            {
                foreach (var recording in
                         _numberAudioComposer.Compose(number))
                {
                    gather.Play(
                        BuildRecordingUri(
                            recordingBaseUrl,
                            $"{RecordingFiles.Numbers.Folder}/{recording}"));
                }

                return;
            }

            gather.Say(
                number.ToString(
                    CultureInfo.InvariantCulture));
        }
        #endregion

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

        #region buildRecordingUriBak
        //private static Uri BuildRecordingUri(string recordingBaseUrl, string relativePath)
        //{
        //    var normalizedBaseUrl = recordingBaseUrl.TrimEnd('/');

        //    var normalizedPath = relativePath
        //        .Replace("\\", "/")
        //        .TrimStart('/');

        //    var encodedSegments = normalizedPath
        //        .Split(
        //            '/',
        //            StringSplitOptions.RemoveEmptyEntries)
        //        .Select(Uri.EscapeDataString);

        //    var encodedPath = string.Join("/", encodedSegments);

        //    return new Uri(
        //        $"{normalizedBaseUrl}/{encodedPath}",
        //        UriKind.Absolute);
        //}
        #endregion

        private Uri BuildRecordingUri(string recordingBaseUrl, string relativePath)
        {
            var applicationBaseUrl = RemoveAudioSuffix(recordingBaseUrl);

            var promptUrl =
                _audioPromptUrlProvider.GetPromptUrl(
                    relativePath,
                    applicationBaseUrl);

            return new Uri(
                promptUrl,
                UriKind.Absolute);
        }

        private static string RemoveAudioSuffix(string recordingBaseUrl)
        {
            var normalizedUrl = recordingBaseUrl.TrimEnd('/');

            const string audioSuffix = "/audio";

            if (normalizedUrl.EndsWith(
                    audioSuffix,
                    StringComparison.OrdinalIgnoreCase))
            {
                return normalizedUrl[
                    ..^audioSuffix.Length];
            }

            return normalizedUrl;
        }

        private static Uri CreateAbsoluteUri(string url)
        {
            return new Uri(url, UriKind.Absolute);
        }
    }
}