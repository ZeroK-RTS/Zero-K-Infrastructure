using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace ZeroKWeb
{
    /// <summary>
    /// Steam's OpenID 2.0 sign-in, written out because DotNetOpenAuth - which MVC 5 uses for it -
    /// has no .NET Core version and is not coming back.
    ///
    /// **No ASP.NET Core types on purpose.** Everything here is strings, a dictionary and an
    /// HttpClient, so Tests.Portable can link this one file and drive every branch, including the
    /// ones that matter: a forged claimed_id, a provider that says is_valid:false, a return_to
    /// pointing somewhere else. The web plumbing that reads the query string and writes the
    /// cookie is in SteamOpenIdCore.cs and stays thin.
    ///
    /// **What this is NOT verified against.** There is no test here that talks to Steam. The
    /// protocol is exercised against a stub, which proves the logic and says nothing about
    /// whether Steam's live endpoint behaves as documented. That is the one thing a live round
    /// trip would add, and it cannot run in CI.
    ///
    /// The security of the whole exchange rests on one step: <see cref="VerifyAsync"/> POSTs the
    /// parameters back to Steam with mode=check_authentication and requires <c>is_valid:true</c>.
    /// Everything in the query string is attacker-supplied until that answer comes back - the
    /// claimed_id most of all, which is the user's identity. A version that skipped this and
    /// simply read openid.claimed_id would work perfectly for honest users and let anyone sign
    /// in as anyone.
    /// </summary>
    public static class SteamOpenIdProtocol
    {
        public const string Endpoint = "https://steamcommunity.com/openid/login";
        public const string Namespace = "http://specs.openid.net/auth/2.0";
        private const string IdentifierSelect = "http://specs.openid.net/auth/2.0/identifier_select";

        /// <summary>The URL to send the browser to. Steam requires no registration or secret.</summary>
        public static string BuildRedirectUrl(string returnTo, string realm)
        {
            if (string.IsNullOrEmpty(returnTo)) throw new ArgumentNullException(nameof(returnTo));
            if (string.IsNullOrEmpty(realm)) throw new ArgumentNullException(nameof(realm));

            var query = new[]
            {
                ("openid.ns", Namespace),
                ("openid.mode", "checkid_setup"),
                ("openid.return_to", returnTo),
                ("openid.realm", realm),
                ("openid.identity", IdentifierSelect),
                ("openid.claimed_id", IdentifierSelect),
            };
            return Endpoint + "?" + string.Join("&", query.Select(
                p => Uri.EscapeDataString(p.Item1) + "=" + Uri.EscapeDataString(p.Item2)));
        }

        /// <summary>
        /// Turns the parameters Steam sent back into a result. <paramref name="expectedReturnToPrefix"/>
        /// is this site's own origin; a signed return_to that does not start with it is refused
        /// even though Steam signed it, because it should not have been possible to obtain.
        ///
        /// Returns null when this is not an OpenID callback at all, which is how the caller tells
        /// an ordinary password post from a return from Steam.
        /// </summary>
        public static async Task<SteamOpenIdResult> CompleteAsync(
            IDictionary<string, string> parameters, string expectedReturnToPrefix, HttpMessageHandler handler)
        {
            if (parameters == null) return null;
            if (!parameters.TryGetValue("openid.mode", out var mode) || string.IsNullOrEmpty(mode)) return null;

            if (mode == "cancel") return new SteamOpenIdResult { Status = SteamOpenIdStatus.Canceled };
            if (mode != "id_res") return Failed("openid.mode was " + mode);

            if (!parameters.TryGetValue("openid.ns", out var ns) || ns != Namespace)
                return Failed("openid.ns was not OpenID 2.0");

            // Signed by Steam, and checked because Steam only signs a return_to inside the realm
            // this site asked for - so one pointing elsewhere means the exchange is not ours.
            if (!parameters.TryGetValue("openid.return_to", out var returnTo)
                || string.IsNullOrEmpty(returnTo)
                || !returnTo.StartsWith(expectedReturnToPrefix, StringComparison.OrdinalIgnoreCase))
                return Failed("openid.return_to is not this site");

            // THE step. Nothing below is trustworthy without it.
            if (!await VerifyAsync(parameters, handler)) return Failed("Steam did not confirm the assertion");

            if (!parameters.TryGetValue("openid.claimed_id", out var claimedId))
                return Failed("no openid.claimed_id");

            var steamId = ParseSteamId(claimedId);
            if (steamId == null) return Failed("openid.claimed_id is not a Steam identifier: " + claimedId);

            return new SteamOpenIdResult
            {
                Status = SteamOpenIdStatus.Authenticated,
                SteamID = steamId.Value,
                // Read out of the SIGNED return_to rather than the raw query string, so it
                // carries the same protection as the rest of the assertion. It is fed to a
                // redirect, which is exactly the parameter an open redirect would ride in on.
                Referer = ReadReferer(returnTo),
            };
        }

        /// <summary>
        /// Asks Steam whether it really signed this. The parameters go back verbatim with the
        /// mode swapped, and the answer has to be an <c>is_valid:true</c> LINE - a body merely
        /// containing that text somewhere is not enough, and a body saying is_valid:false must
        /// never pass.
        /// </summary>
        public static async Task<bool> VerifyAsync(IDictionary<string, string> parameters, HttpMessageHandler handler)
        {
            var form = parameters
                .Where(p => p.Key.StartsWith("openid.", StringComparison.Ordinal))
                .ToDictionary(p => p.Key, p => p.Value);
            form["openid.mode"] = "check_authentication";

            using (var client = new HttpClient(handler, disposeHandler: false))
            {
                var response = await client.PostAsync(Endpoint, new FormUrlEncodedContent(form));
                if (!response.IsSuccessStatusCode) return false;
                var body = await response.Content.ReadAsStringAsync();
                return body
                    .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Any(line => line.Trim() == "is_valid:true");
            }
        }

        /// <summary>
        /// A Steam claimed_id is exactly https://steamcommunity.com/openid/id/{17 digits}. Any
        /// other shape is refused rather than coaxed into a number - the original took the last
        /// path segment of whatever came back and parsed it.
        /// </summary>
        public static ulong? ParseSteamId(string claimedId)
        {
            const string prefix = "https://steamcommunity.com/openid/id/";
            if (string.IsNullOrEmpty(claimedId)) return null;
            if (!claimedId.StartsWith(prefix, StringComparison.Ordinal)) return null;

            var digits = claimedId.Substring(prefix.Length);
            if (digits.Length != 17 || !digits.All(char.IsDigit)) return null;
            return ulong.TryParse(digits, out var steamId) ? steamId : (ulong?)null;
        }

        /// <summary>The referer this site put into return_to when it started the exchange.</summary>
        public static string ReadReferer(string returnTo)
        {
            var question = returnTo.IndexOf('?');
            if (question < 0) return null;
            foreach (var pair in returnTo.Substring(question + 1).Split('&'))
            {
                var equals = pair.IndexOf('=');
                if (equals < 0) continue;
                if (Uri.UnescapeDataString(pair.Substring(0, equals)) == "referer")
                    return Uri.UnescapeDataString(pair.Substring(equals + 1));
            }
            return null;
        }

        private static SteamOpenIdResult Failed(string why) =>
            new SteamOpenIdResult { Status = SteamOpenIdStatus.Failed, FailureReason = why };
    }
}
