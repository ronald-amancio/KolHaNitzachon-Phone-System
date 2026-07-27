using KolHaNitzachon.PhoneSystem.Application.Interfaces.IVR;
using KolHaNitzachon.PhoneSystem.Application.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KolHaNitzachon.PhoneSystem.Infrastructure.Services.IVR
{
    public sealed class InMemoryIvrCallSessionStore
    : IIvrCallSessionStore
    {
        private static readonly TimeSpan SessionLifetime =
            TimeSpan.FromMinutes(30);

        private readonly ConcurrentDictionary<string, IvrCallSession>
            _sessions = new(StringComparer.Ordinal);

        private readonly ILogger<InMemoryIvrCallSessionStore> _logger;

        public InMemoryIvrCallSessionStore(
            ILogger<InMemoryIvrCallSessionStore> logger)
        {
            _logger = logger;
        }

        public IvrCallSession GetOrCreate(
            string callSid,
            string? callerPhoneNumber = null)
        {
            if (string.IsNullOrWhiteSpace(callSid))
            {
                throw new ArgumentException(
                    "A SignalWire CallSid is required.",
                    nameof(callSid));
            }

            var normalizedCallSid = callSid.Trim();

            var session = _sessions.GetOrAdd(
                normalizedCallSid,
                key =>
                {
                    var now = DateTime.UtcNow;

                    _logger.LogInformation(
                        "Creating IVR session for CallSid={CallSid}",
                        key);

                    return new IvrCallSession
                    {
                        CallSid = key,
                        CallerPhoneNumber = NormalizePhoneNumber(
                            callerPhoneNumber),
                        CreatedAtUtc = now,
                        LastUpdatedAtUtc = now,
                        ExpiresAtUtc = now.Add(SessionLifetime)
                    };
                });

            if (!string.IsNullOrWhiteSpace(callerPhoneNumber))
            {
                session.CallerPhoneNumber =
                    NormalizePhoneNumber(callerPhoneNumber);
            }

            RefreshExpiration(session);

            return session;
        }

        public bool TryGet(
            string callSid,
            out IvrCallSession? session)
        {
            session = null;

            if (string.IsNullOrWhiteSpace(callSid))
            {
                return false;
            }

            if (!_sessions.TryGetValue(
                    callSid.Trim(),
                    out var storedSession))
            {
                return false;
            }

            if (storedSession.ExpiresAtUtc <= DateTime.UtcNow)
            {
                _sessions.TryRemove(
                    callSid.Trim(),
                    out _);

                _logger.LogInformation(
                    "Removed expired IVR session. CallSid={CallSid}",
                    callSid);

                return false;
            }

            RefreshExpiration(storedSession);

            session = storedSession;

            return true;
        }

        public void Update(IvrCallSession session)
        {
            ArgumentNullException.ThrowIfNull(session);

            if (string.IsNullOrWhiteSpace(session.CallSid))
            {
                throw new ArgumentException(
                    "The IVR session must have a CallSid.",
                    nameof(session));
            }

            RefreshExpiration(session);

            _sessions.AddOrUpdate(session.CallSid, session, (_, _) => session);

            _logger.LogDebug(
                "Updated IVR session. CallSid={CallSid}, " +
                "Step={Step}, DonationType={DonationType}, " +
                "DonationAmount={DonationAmount}",
                session.CallSid,
                session.CurrentStep,
                session.DonationType,
                session.DonationAmount);
        }

        public bool Remove(string callSid)
        {
            if (string.IsNullOrWhiteSpace(callSid))
            {
                return false;
            }

            var removed = _sessions.TryRemove(callSid.Trim(), out _);

            if (removed)
            {
                _logger.LogInformation("Removed IVR session. CallSid={CallSid}", callSid);
            }

            return removed;
        }

        public int RemoveExpiredSessions()
        {
            var now = DateTime.UtcNow;
            var removedCount = 0;

            foreach (var sessionEntry in _sessions)
            {
                if (sessionEntry.Value.ExpiresAtUtc > now)
                {
                    continue;
                }

                if (_sessions.TryRemove(sessionEntry.Key, out _))
                {
                    removedCount++;
                }
            }

            if (removedCount > 0)
            {
                _logger.LogInformation("Removed {SessionCount} expired IVR sessions.", removedCount);
            }

            return removedCount;
        }

        private static void RefreshExpiration(IvrCallSession session)
        {
            var now = DateTime.UtcNow;

            session.LastUpdatedAtUtc = now;
            session.ExpiresAtUtc = now.Add(SessionLifetime);
        }

        private static string? NormalizePhoneNumber(string? phoneNumber)
        {
            return string.IsNullOrWhiteSpace(phoneNumber)
                ? null
                : phoneNumber.Trim();
        }
    }
}