using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KolHaNitzachon.PhoneSystem.Application.Models
{
    public enum DonationType
    {
        Unknown = 0,

        DonateToAllPerDay = 1,

        DonateToAllSameAmount = 2,

        SponsorSpecificRecipient = 3
    }
}