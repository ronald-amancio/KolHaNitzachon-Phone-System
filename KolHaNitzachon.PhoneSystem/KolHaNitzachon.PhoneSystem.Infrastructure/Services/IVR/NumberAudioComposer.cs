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
        private static readonly IReadOnlyDictionary<int, string> Units =
            new Dictionary<int, string>
            {
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
            if (number is < 1 or > 1000)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(number),
                    "The prerecorded number range supports values from 1 to 1000.");
            }

            var recordings = new List<string>();

            AppendNumber(number, recordings);

            return recordings;
        }

        private static void AppendNumber(int number, ICollection<string> recordings)
        {
            if (number == 1000)
            {
                recordings.Add("one.mp3");
                recordings.Add("thousand.mp3");
                return;
            }

            if (number >= 100)
            {
                var hundreds = number / 100;
                var remainder = number % 100;

                recordings.Add(Units[hundreds]);
                recordings.Add("hundred.mp3");

                if (remainder > 0)
                {
                    recordings.Add("and.mp3");
                    AppendBelowOneHundred(remainder, recordings);
                }

                return;
            }

            AppendBelowOneHundred(number, recordings);
        }

        private static void AppendBelowOneHundred(int number, ICollection<string> recordings)
        {
            if (number <= 0)
            {
                return;
            }

            if (number < 20)
            {
                recordings.Add(Units[number]);
                return;
            }

            var tensValue = number / 10 * 10;
            var unitValue = number % 10;

            recordings.Add(Tens[tensValue]);

            if (unitValue > 0)
            {
                recordings.Add(Units[unitValue]);
            }
        }
    }
}