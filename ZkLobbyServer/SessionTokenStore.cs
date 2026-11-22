using System;
using System.Collections.Concurrent;
using System.Linq;

namespace ZkLobbyServer
{
    /// <summary>
    /// The single sign-on tokens, with a lifetime.
    ///
    /// A token is issued in the lobby login response and redeemed once by the website, which then
    /// sets its own auth cookie - so in practice it is used within moments of being handed over.
    /// It used to live forever: until redeemed, or until the account's tokens were cleared. A
    /// bearer credential that signs someone into the website and never expires is the one thing
    /// left on the Phase 1 list after the wire was made trustworthy.
    ///
    /// **The awkward part, stated rather than hidden.** The client receives the token once, at
    /// login (LobbyClient/TasClient.cs), and nothing refreshes it. So a client connected longer
    /// than <see cref="Lifetime"/> will find the website no longer signs it in automatically -
    /// the user sees a login page and reconnecting to the lobby fixes it. That is the cost of
    /// bounding the credential, it is recoverable and loses nothing, and the lifetime is a MiscVar
    /// so it can be moved without a deploy.
    ///
    /// Expiry is enforced on REDEEM rather than by a timer, so there is no moment where a token
    /// is gone but the dictionary still says otherwise. Pruning is opportunistic, on issue.
    /// </summary>
    public class SessionTokenStore
    {
        public const string LifetimeHoursKey = "LobbySessionTokenLifetimeHours";
        public static readonly TimeSpan DefaultLifetime = TimeSpan.FromHours(24);

        private readonly ConcurrentDictionary<string, Entry> tokens =
            new ConcurrentDictionary<string, Entry>();
        private readonly Func<DateTime> utcNow;

        public SessionTokenStore(TimeSpan? lifetime = null, Func<DateTime> utcNow = null)
        {
            Lifetime = lifetime ?? DefaultLifetime;
            this.utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public TimeSpan Lifetime { get; }

        public int Count => tokens.Count;

        /// <summary>Reads the configured lifetime, falling back to the default for anything unusable.</summary>
        public static TimeSpan ParseLifetime(string configuredHours)
        {
            double hours;
            if (!double.TryParse(configuredHours, System.Globalization.NumberStyles.Float,
                                 System.Globalization.CultureInfo.InvariantCulture, out hours)) return DefaultLifetime;
            // Zero or negative would make every token dead on arrival, which is not a
            // configuration anyone means; it reads as "off" and is not honoured as one.
            return hours <= 0 ? DefaultLifetime : TimeSpan.FromHours(hours);
        }

        public void Add(string token, int accountID)
        {
            if (string.IsNullOrEmpty(token)) return;
            Prune();
            tokens[token] = new Entry { AccountID = accountID, IssuedUtc = utcNow() };
        }

        /// <summary>
        /// The account this token stands for, or null. Single use: the token is removed whether or
        /// not it turned out to be valid, because an expired one is not going to become valid and
        /// leaving it would make this a way to test tokens for existence.
        /// </summary>
        public int? Redeem(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;

            Entry entry;
            if (!tokens.TryRemove(token, out entry)) return null;
            if (utcNow() - entry.IssuedUtc > Lifetime) return null;
            return entry.AccountID;
        }

        public void RemoveForAccount(int accountID)
        {
            foreach (var token in tokens.Where(x => x.Value.AccountID == accountID).Select(x => x.Key).ToList())
            {
                Entry removed;
                tokens.TryRemove(token, out removed);
            }
        }

        private void Prune()
        {
            var cutoff = utcNow() - Lifetime;
            foreach (var token in tokens.Where(x => x.Value.IssuedUtc < cutoff).Select(x => x.Key).ToList())
            {
                Entry removed;
                tokens.TryRemove(token, out removed);
            }
        }

        private struct Entry
        {
            public int AccountID;
            public DateTime IssuedUtc;
        }
    }
}
