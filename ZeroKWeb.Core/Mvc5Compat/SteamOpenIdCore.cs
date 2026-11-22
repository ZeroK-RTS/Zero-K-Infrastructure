using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;

namespace ZeroKWeb
{
    /// <summary>
    /// The .NET 9 half of the Steam sign-in twin pair. Its MVC 5 twin is
    /// Zero-K.info/AppCode/SteamOpenId.cs and forwards to DotNetOpenAuth, which has no .NET Core
    /// version - that is the whole reason this exists.
    ///
    /// Only web plumbing lives here: read the query string, build a return_to, hand back a
    /// redirect. The exchange itself is in SteamOpenIdProtocol.cs, which has no ASP.NET Core
    /// types precisely so Tests.Portable can drive every branch of it, forgeries included.
    ///
    /// **The realm is this request's own origin.** Steam requires no registration, and return_to
    /// has to sit inside the realm, so deriving both from the live request keeps them consistent
    /// wherever the site runs - and the same origin is what the protocol then checks return_to
    /// against on the way back. Behind a reverse proxy this needs ForwardedHeaders configured,
    /// or Scheme is http when the browser used https and Steam will refuse the return_to.
    /// </summary>
    public static class SteamOpenId
    {
        /// <summary>
        /// Overridable for tests and nothing else. Production leaves it null and gets an ordinary
        /// handler; Tests.Portable drives SteamOpenIdProtocol directly rather than through here.
        /// </summary>
        public static HttpMessageHandler HandlerForTests;

        public static SteamOpenIdResult TryCompleteLogin(Controller controller)
        {
            var request = controller?.HttpContext?.Request;
            if (request == null) return null;

            var parameters = request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
            var handler = HandlerForTests ?? new HttpClientHandler();
            try
            {
                return SteamOpenIdProtocol
                    .CompleteAsync(parameters, Origin(controller), handler)
                    .GetAwaiter().GetResult();
            }
            finally
            {
                if (HandlerForTests == null) handler.Dispose();
            }
        }

        public static ActionResult BeginLogin(Controller controller, string referer)
        {
            var origin = Origin(controller);
            var returnTo = origin + controller.Url.Action("Logon", "Home");
            if (!string.IsNullOrEmpty(referer))
                returnTo += "?referer=" + Uri.EscapeDataString(referer);

            return new RedirectResult(SteamOpenIdProtocol.BuildRedirectUrl(returnTo, origin + "/"));
        }

        private static string Origin(Controller controller)
        {
            var request = controller.HttpContext.Request;
            return request.Scheme + "://" + request.Host.Value;
        }
    }
}
