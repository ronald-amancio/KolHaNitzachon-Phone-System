using KolHaNitzachon.PhoneSystem.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KolHaNitzachon.PhoneSystem.Application.Interfaces.IVR
{
    public interface IIvrCallSessionStore
    {
        IvrCallSession GetOrCreate(string callSid, string? callerPhoneNumber = null);
        bool TryGet(string callSid, out IvrCallSession? session);
        void Update(IvrCallSession session);
        bool Remove(string callSid);
        int RemoveExpiredSessions();
    }
}