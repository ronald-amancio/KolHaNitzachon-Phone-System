using KolHaNitzachon.PhoneSystem.Application.Interfaces.IVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KolHaNitzachon.PhoneSystem.Infrastructure.Services.IVR
{
    public sealed class NumberAudioComposer : INumberAudioComposer
    {
        private const decimal MaximumDonationAmount = 999_999m;

        private static readonly IReadOnlyDictionary<int, string> Units =
            new Dictionary<int, string>
            {
                [0] = "zero.mp3",
                [1] = "one.mp3",
                [2] = "two.mp3",
                [3] = "three.mp3",
                [4] = "four.mp3",
                [5] = "five.mp3",
                [6] = "six.mp3",
                [7] = "seven.mp3",
                [8] = "eight.mp3",
                [9] = "nine.mp3",
                [10] = "ten.mp3",
                [11] = "eleven.mp3",
                [12] = "twelve.mp3",
                [13] = "thirteen.mp3",
                [14] = "fourteen.mp3",
                [15] = "fifteen.mp3",
                [16] = "sixteen.mp3",
                [17] = "seventeen.mp3",
                [18] = "eighteen.mp3",
                [19] = "nineteen.mp3"
            };

        private static readonly IReadOnlyDictionary<int, string> Tens =
            new Dictionary<int, string>
            {
                [20] = "twenty.mp3",
                [30] = "thirty.mp3",
                [40] = "forty.mp3",
                [50] = "fifty.mp3",
                [60] = "sixty.mp3",
                [70] = "seventy.mp3",
                [80] = "eighty.mp3",
                [90] = "ninety.mp3"
            };

        public IReadOnlyList<string> Compose(int number)
        {
            if (number is < 0 or > (int)MaximumDonationAmount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(number),
                    $"Recorded-number playback supports values from 0 " +
                    $"to {MaximumDonationAmount:N0}.");
            }

            var recordings = new List<string>();

            AppendNumber(number, recordings);

            return recordings;
        }

        private static void AppendNumber(int number, ICollection<string> recordings)
        {
            if (number < 100)
            {
                AppendBelowOneHundred(number, recordings);
                return;
            }

            if (number < 1000)
            {
                AppendBelowOneThousand(number, recordings);
                return;
            }

            var thousands = number / 1000;
            var remainder = number % 1000;

            AppendNumber(thousands, recordings);
            recordings.Add("thousand.mp3");

            if (remainder <= 0)
            {
                return;
            }

            // Example: 1,005 → one thousand and five.
            if (remainder < 100)
            {
                recordings.Add("and.mp3");
            }

            AppendBelowOneThousand(remainder, recordings);
        }

        private static void AppendBelowOneThousand(int number, ICollection<string> recordings)
        {
            if (number < 100)
            {
                AppendBelowOneHundred(number, recordings);
                return;
            }

            var hundreds = number / 100;
            var remainder = number % 100;

            recordings.Add(Units[hundreds]);
            recordings.Add("hundred.mp3");

            if (remainder > 0)
            {
                recordings.Add("and.mp3");
                AppendBelowOneHundred(remainder, recordings);
            }
        }

        private static void AppendBelowOneHundred(int number, ICollection<string> recordings)
        {
            if (number == 0)
            {
                // Only pronounce zero when the entire value is zero.
                if (recordings.Count == 0)
                {
                    recordings.Add(Units[0]);
                }

                return;
            }

            if (number < 20)
            {
                recordings.Add(Units[number]);
                return;
            }

            var tensValue = number / 10 * 10;
            var unitsValue = number % 10;

            recordings.Add(Tens[tensValue]);

            if (unitsValue > 0)
            {
                recordings.Add(Units[unitsValue]);
            }
        }
    }
}