using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KolHaNitzachon.PhoneSystem.Infrastructure.Services.IVR
{
    public static class RecordingFiles
    {
        #region NotInUsed
        //public const string MainMenu = "MainMenu.mp3";
        //public const string SponsorAllMenu = "SponsorAllMenu.mp3";
        //public const string SponsorSpecificMenu = "SponsorSpecificMenu.mp3";
        //public const string EnterContestantCode = "EnterContestantCode.mp3";
        //public const string InvalidOption = "InvalidOption.mp3";
        //public const string ContestantNotFound = "ContestantNotFound.mp3";

        //// Chained contestant-description recordings
        //public const string Contestant = "Contestant.mp3";
        //public const string HasBeenCompetingFor = "HasBeenCompetingFor.mp3";
        //public const string Days = "Days.mp3";
        //public const string CodeNumberToSponsor = "CodeNumberToSponsor.mp3";
        //public const string EnterPledgeAmount = "EnterPledgeAmount.mp3";

        //// Used after the caller enters the amount.
        //public const string EnterPaymentInformation =
        //    "EnterPaymentInformation.mp3";
        #endregion

        public static class MainMenu
        {
            public const string Part2 = "MainMenu/main-menu-part-2.mp3";
            public const string Part3 = "MainMenu/main-menu-part-3.mp3";
        }

        public static class SponsorAll
        {
            public const string Menu = "Option1/donate-to-all-menu.mp3";

            public const string EnterAmount =
                "Option1-1/enter-donation-amount.mp3";
        }

        public static class Recipient
        {
            public const string EnterCode = "Option2/enter-recipient-code.mp3";
            public const string NotFound = "Option2/recipient-not-found.mp3";
            public const string RecipientPrefix = "Option2/recipient.mp3";
            public const string HasBeenCompetingFor = "Option2/has-been-competing-for.mp3";
            public const string Days = "Option2/days.mp3";
            public const string CodeNumberToSponsor = "Option2/code-number-to-sponsor.mp3";
            public const string EnterPledgeAmount = "Option2/enter-pledge-amount.mp3";
        }

        public static class Payment
        {
            public const string Successful = "charge-successful.mp3";

            public const string NotSuccessful =
                "charge-not-successful.mp3";

            public const string MinimumDonation =
                "minimum-donation.mp3";
        }

        public static class Numbers
        {
            public const string Folder = "numbers1-1000";
        }
    }
}