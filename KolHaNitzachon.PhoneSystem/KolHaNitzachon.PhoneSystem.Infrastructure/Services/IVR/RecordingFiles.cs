using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KolHaNitzachon.PhoneSystem.Infrastructure.Services.IVR
{
    public static class RecordingFiles
    {
        public const string MainMenu = "MainMenu.mp3";
        public const string SponsorAllMenu = "SponsorAllMenu.mp3";
        public const string SponsorSpecificMenu = "SponsorSpecificMenu.mp3";
        public const string EnterContestantCode = "EnterContestantCode.mp3";
        public const string InvalidOption = "InvalidOption.mp3";
        public const string ContestantNotFound = "ContestantNotFound.mp3";

        // Chained contestant-description recordings
        public const string Contestant = "Contestant.mp3";
        public const string HasBeenCompetingFor = "HasBeenCompetingFor.mp3";
        public const string Days = "Days.mp3";
        public const string CodeNumberToSponsor = "CodeNumberToSponsor.mp3";
        public const string EnterPledgeAmount = "EnterPledgeAmount.mp3";

        // Used after the caller enters the amount.
        public const string EnterPaymentInformation =
            "EnterPaymentInformation.mp3";
    }
}