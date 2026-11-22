using System;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OpenId;
using DotNetOpenAuth.OpenId.RelyingParty;

namespace ZeroKWeb
{
    /// <summary>
    /// The MVC 5 half of the Steam sign-in twin pair: DotNetOpenAuth, exactly as HomeController
    /// used it before the seam existed.
    ///
    /// **This is the half the live site runs, and it is deliberately unchanged.** DotNetOpenAuth
    /// has no .NET Core version, so the port cannot use it and implements the protocol directly -
    /// see ZeroKWeb.Core/Mvc5Compat/SteamOpenIdProtocol.cs. Putting a seam between the controller
    /// and the library is what lets that happen without the running site's authentication being
    /// rewritten by a port.
    ///
    /// The only structural change to the original: the relying party is created inside these two
    /// methods rather than once in Logon and passed along. Same calls, same order.
    /// </summary>
    public static class SteamOpenId
    {
        /// <summary>Null when this request is not a return from Steam.</summary>
        public static SteamOpenIdResult TryCompleteLogin(Controller controller)
        {
            var response = new OpenIdRelyingParty().GetResponse();
            if (response == null) return null;

            switch (response.Status)
            {
                case AuthenticationStatus.Authenticated:
                    var steamIDStr = response.FriendlyIdentifierForDisplay.Split('/').LastOrDefault();
                    ulong steamID;
                    if (ulong.TryParse(steamIDStr, out steamID))
                        return new SteamOpenIdResult
                        {
                            Status = SteamOpenIdStatus.Authenticated,
                            SteamID = steamID,
                            Referer = response.GetCallbackArgument("referer"),
                        };
                    return new SteamOpenIdResult { Status = SteamOpenIdStatus.Failed, FailureReason = "unparseable steam id" };

                case AuthenticationStatus.Canceled:
                    return new SteamOpenIdResult { Status = SteamOpenIdStatus.Canceled };

                default:
                    return new SteamOpenIdResult { Status = SteamOpenIdStatus.Failed, FailureReason = response.Status.ToString() };
            }
        }

        /// <summary>Null when Steam could not be reached, which the caller reports as offline.</summary>
        public static ActionResult BeginLogin(Controller controller, string referer)
        {
            var openid = new OpenIdRelyingParty();
            IAuthenticationRequest request = null;
            var tries = 3;
            while (request == null && tries > 0)
                try
                {
                    tries--;
                    request = openid.CreateRequest(Identifier.Parse("https://steamcommunity.com/openid/"));
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Steam openid CreateRequest has failed: {0}", ex);
                }
            if (request == null) return null;
            if (!string.IsNullOrEmpty(referer)) request.SetCallbackArgument("referer", referer);
            return request.RedirectingResponse.AsActionResultMvc5();
        }
    }
}
