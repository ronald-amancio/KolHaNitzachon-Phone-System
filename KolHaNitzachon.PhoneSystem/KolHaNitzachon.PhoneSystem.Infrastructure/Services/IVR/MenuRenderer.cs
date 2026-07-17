using KolHaNitzachon.PhoneSystem.Application.Interfaces.External;
using KolHaNitzachon.PhoneSystem.Application.Interfaces.IVR;
using KolHaNitzachon.PhoneSystem.Domain.Entities;
using Twilio.TwiML;
using Twilio.TwiML.Voice;

namespace KolHaNitzachon.PhoneSystem.Infrastructure.Services.IVR
{
    public class MenuRenderer : IMenuRenderer
    {
        public VoiceResponse RenderMainMenu(
            string actionUrl,
            string recordingBaseUrl)
        {
            return BuildSingleDigitMenu(
                recordingFile: RecordingFiles.MainMenu,
                actionUrl: actionUrl,
                recordingBaseUrl: recordingBaseUrl);
        }

        public VoiceResponse RenderSponsorAllMenu(
            string actionUrl,
            string recordingBaseUrl)
        {
            return BuildSingleDigitMenu(
                recordingFile: RecordingFiles.SponsorAllMenu,
                actionUrl: actionUrl,
                recordingBaseUrl: recordingBaseUrl);
        }

        public VoiceResponse RenderSponsorSpecificMenu(
            string actionUrl,
            string recordingBaseUrl)
        {
            return BuildSingleDigitMenu(
                recordingFile: RecordingFiles.SponsorSpecificMenu,
                actionUrl: actionUrl,
                recordingBaseUrl: recordingBaseUrl);
        }

        public VoiceResponse RenderEnterContestantCode(
            string actionUrl,
            string recordingBaseUrl)
        {
            var response = new VoiceResponse();

            var gather = new Gather(
                action: new Uri(actionUrl, UriKind.Absolute),
                method: "POST",
                timeout: 10,
                finishOnKey: "#");

            gather.Play(
                BuildLocalRecordingUri(
                    recordingBaseUrl,
                    RecordingFiles.EnterContestantCode));

            response.Append(gather);

            // Replay the prompt when no input is received.
            response.Redirect(
                new Uri(actionUrl, UriKind.Absolute),
                method: "POST");

            return response;
        }

        public VoiceResponse RenderContestantList(
            IEnumerable<Recipient> recipients,
            string actionUrl,
            string recordingBaseUrl)
        {
            var response = new VoiceResponse();

            var gather = new Gather(
                action: new Uri(actionUrl, UriKind.Absolute),
                method: "POST",
                timeout: 15,
                finishOnKey: "#");

            var activeRecipients = recipients
                .Where(IsRecipientActive)
                .OrderBy(x => x.Name)
                .ToList();

            if (activeRecipients.Count == 0)
            {
                gather.Say(
                    "There are currently no active contestants.");

                response.Append(gather);
                response.Redirect(
                    new Uri(actionUrl, UriKind.Absolute),
                    method: "POST");

                return response;
            }

            foreach (var recipient in activeRecipients)
            {
                AppendContestantDescription(
                    gather,
                    recipient,
                    recordingBaseUrl);
            }

            gather.Play(
                BuildLocalRecordingUri(
                    recordingBaseUrl,
                    RecordingFiles.EnterContestantCode));

            response.Append(gather);

            return response;
        }

        public VoiceResponse RenderContestantDonation(
            Recipient recipient,
            string actionUrl,
            string recordingBaseUrl)
        {
            var response = new VoiceResponse();

            var gather = new Gather(
                action: new Uri(actionUrl, UriKind.Absolute),
                method: "POST",
                timeout: 10,
                finishOnKey: "#");

            /*
             * Yanky's required chain:
             *
             * 1. Contestant
             * 2. contestant name recording
             * 3. has been competing for
             * 4. number-of-days recording
             * 5. days
             * 6. code number to sponsor
             * 7. contestant code using TTS
             *
             * This version then plays the prompt asking for the pledge amount.
             */

            AppendContestantDescription(
                gather,
                recipient,
                recordingBaseUrl);

            gather.Play(
                BuildLocalRecordingUri(
                    recordingBaseUrl,
                    RecordingFiles.EnterPledgeAmount));

            response.Append(gather);

            // Repeat the contestant information if no amount is entered.
            response.Redirect(
                new Uri(actionUrl, UriKind.Absolute),
                method: "POST");

            return response;
        }

        public VoiceResponse RenderPledgeConfirmation(
            Recipient recipient,
            decimal pledgeAmount,
            string actionUrl,
            string recordingBaseUrl)
        {
            var response = new VoiceResponse();

            // Yanky requested TTS for this dynamic amount.
            response.Say(
                $"You have selected to pledge {pledgeAmount:0.##} dollars.");

            response.Play(
                BuildLocalRecordingUri(
                    recordingBaseUrl,
                    RecordingFiles.EnterPaymentInformation));

            /*
             * Payment collection will be implemented next.
             * For now, redirecting to the main menu avoids ending the call
             * unexpectedly during testing.
             */
            response.Redirect(
                new Uri(actionUrl, UriKind.Absolute),
                method: "POST");

            return response;
        }

        public VoiceResponse RenderInvalidOption(
            string actionUrl,
            string recordingBaseUrl)
        {
            var response = new VoiceResponse();

            response.Play(
                BuildLocalRecordingUri(
                    recordingBaseUrl,
                    RecordingFiles.InvalidOption));

            response.Redirect(
                new Uri(actionUrl, UriKind.Absolute),
                method: "POST");

            return response;
        }

        public VoiceResponse RenderContestantNotFound(
            string actionUrl,
            string recordingBaseUrl)
        {
            var response = new VoiceResponse();

            response.Play(
                BuildLocalRecordingUri(
                    recordingBaseUrl,
                    RecordingFiles.ContestantNotFound));

            response.Redirect(
                new Uri(actionUrl, UriKind.Absolute),
                method: "POST");

            return response;
        }

        private VoiceResponse BuildSingleDigitMenu(
            string recordingFile,
            string actionUrl,
            string recordingBaseUrl)
        {
            var response = new VoiceResponse();

            var gather = new Gather(
                action: new Uri(actionUrl, UriKind.Absolute),
                method: "POST",
                numDigits: 1,
                timeout: 10);

            gather.Play(
                BuildLocalRecordingUri(
                    recordingBaseUrl,
                    recordingFile));

            response.Append(gather);

            // Replays the same menu when the caller provides no input.
            response.Redirect(
                new Uri(actionUrl, UriKind.Absolute),
                method: "POST");

            return response;
        }

        private static void AppendContestantDescription(Gather gather, Recipient recipient, string recordingBaseUrl)
        {
            // 1. "Contestant"
            gather.Play(BuildLocalRecordingUri(recordingBaseUrl, RecordingFiles.Contestant));

            // 2. Uploaded contestant-name recording
            if (!string.IsNullOrWhiteSpace(recipient.NameRecordingUrl))
            {
                gather.Play(BuildRecordingUri(recipient.NameRecordingUrl, recordingBaseUrl));
            }
            else
            {
                // Temporary fallback when a contestant has no name recording.
                gather.Say(recipient.Name ?? "Unknown contestant");
            }

            // 3. "has been competing for"
            gather.Play(BuildLocalRecordingUri(recordingBaseUrl, RecordingFiles.HasBeenCompetingFor));

            // 4. Number of days as a prerecorded file.
            var competitionDays = CalculateCompetitionDays(recipient.StartDate);

            gather.Play(new Uri($"{recordingBaseUrl.TrimEnd('/')}/numbers/" + $"{competitionDays}.mp3", UriKind.Absolute));

            // 5. "days"
            gather.Play(BuildLocalRecordingUri( recordingBaseUrl, RecordingFiles.Days));

            // 6. "code number to sponsor"
            gather.Play(BuildLocalRecordingUri(recordingBaseUrl, RecordingFiles.CodeNumberToSponsor));

            // 7. Code using TTS, exactly as Yanky requested.
            gather.Say(FormatCodeForSpeech(recipient.Code));
        }

        private static int CalculateCompetitionDays(DateTime startDate)
        {
            var today = DateTime.UtcNow.Date;
            var normalizedStartDate = startDate.Date;

            if (normalizedStartDate > today)
            {
                return 0;
            }

            /*
             * +1 means the contestant's start day counts as day one.
             * Remove +1 if Yanky wants elapsed full days instead.
             */
            return Math.Max(1, (today - normalizedStartDate).Days + 1);
        }

        private static string FormatCodeForSpeech(int code)
        {
            // Makes SignalWire speak 1234 as "1 2 3 4"
            // rather than "one thousand two hundred thirty-four".
            return string.Join(" ", code.ToString().ToCharArray());
        }

        private static Uri BuildLocalRecordingUri(string recordingBaseUrl, string fileName)
        {
            return new Uri($"{recordingBaseUrl.TrimEnd('/')}/{fileName}", UriKind.Absolute);
        }

        private static Uri BuildRecordingUri(string recordingUrl, string recordingBaseUrl)
        {
            if (Uri.TryCreate(recordingUrl, UriKind.Absolute, out var absoluteUri))
            {
                return absoluteUri;
            }

            /*
             * Supports database values such as:
             *
             * file.mp3
             * recordings/file.mp3
             * /recordings/file.mp3
             */
            var cleanValue = recordingUrl
                .Replace("\\", "/")
                .TrimStart('/');

            if (cleanValue.StartsWith("recordings/", StringComparison.OrdinalIgnoreCase)) {
                cleanValue = cleanValue["recordings/".Length..];
            }

            return new Uri($"{recordingBaseUrl.TrimEnd('/')}/{cleanValue}", UriKind.Absolute);
        }

        private static bool IsRecipientActive(Recipient recipient)
        {
            var today = DateTime.UtcNow.Date;

            return recipient.StartDate.Date <= today && (!recipient.EndDate.HasValue || recipient.EndDate.Value.Date >= today);
        }
    }
}