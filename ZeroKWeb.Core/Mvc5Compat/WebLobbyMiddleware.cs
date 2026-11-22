using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace ZeroKWeb.Compat
{
    /// <summary>
    /// Global.asax's OnPostAcquireRequestState, which is two lines:
    ///
    ///     if (Request.QueryString["weblobby"] != null) Session["weblobby"] = Request.QueryString["weblobby"];
    ///     if (Request.QueryString["zkl"] != null) Session["zkl"] = Request.QueryString["zkl"];
    ///
    /// Only the first is reproduced, and the second is left out on purpose: NOTHING in this
    /// repository reads <c>Session["zkl"]</c>. It is written on every request that carries the
    /// parameter and never looked at again, on both stacks. Porting it would mean setting a cookie
    /// on a visitor's browser to carry a value no code consumes.
    ///
    /// The first IS read - by <see cref="Global.IsWebLobbyAccess"/>, which _SiteLayout.cshtml:114
    /// uses to decide whether to draw the site menu at all. See GlobalCompat for why the flag
    /// lives in a cookie rather than in ASP.NET Core session.
    /// </summary>
    public static class WebLobbyMiddleware
    {
        public static IApplicationBuilder UseWebLobbyFlag(this IApplicationBuilder app) =>
            app.Use(async (context, next) =>
            {
                var asked = context.Request.Query[Global.WebLobbyCookie].ToString();
                var carried = context.Request.Cookies[Global.WebLobbyCookie];

                // Set when asked for, never cleared, which is what Session did: there is no
                // "leave the web lobby" on MVC 5 either. The flag lasts the browsing session.
                if (!string.IsNullOrEmpty(asked) && asked != carried)
                {
                    context.Response.Cookies.Append(Global.WebLobbyCookie, asked, new CookieOptions
                    {
                        HttpOnly = true,
                        SameSite = SameSiteMode.Lax,
                        IsEssential = true,
                    });
                }

                // Resolved once, here, for this request. The cookie is only on the RESPONSE when
                // it has just been set, so a reader looking at Request.Cookies would miss exactly
                // the request the web lobby opens the site with.
                if (!string.IsNullOrEmpty(asked) || !string.IsNullOrEmpty(carried))
                    context.Items[Global.WebLobbyItemKey] = true;

                await next();
            });
    }
}
