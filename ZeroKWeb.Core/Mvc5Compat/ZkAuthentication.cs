using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using PlasmaShared;
using ZkData;

namespace ZeroKWeb.Compat
{
    /// <summary>
    /// Who is signed in, on ASP.NET Core.
    ///
    /// This is the third time authorization has come up in this port and the first time identity
    /// has. AuthCompat made a linked controller's [Auth] mean what it says; this is what makes it
    /// ever answer yes.
    ///
    /// **MVC 5 makes the Account itself the IPrincipal.** Global.asax does
    /// <c>HttpContext.Current.User = acc</c>, which works because ZkData.Account implements
    /// IPrincipal and IIdentity. ASP.NET Core's <c>HttpContext.User</c> is a ClaimsPrincipal and
    /// cannot be an entity, so the two halves are separated: cookie authentication carries the
    /// NAME as a claim, and the middleware below turns that name back into an Account and puts it
    /// in <c>HttpContext.Items</c>, where GlobalCompat already looks for it. 46 views and every
    /// linked controller read <c>Global.Account</c> and none of them change.
    ///
    /// The per-request logic is Global.asax's, in its order, including the parts that are easy to
    /// leave out:
    ///
    /// - the session-token path, so a player arriving from the game client is signed in;
    /// - the **ban check**, which in MVC 5 writes three lines and calls Response.End(). Dropping
    ///   it would have been invisible and would have let a site-banned account browse.
    ///
    /// What is deliberately NOT reproduced is <c>FormsAuthentication.SetAuthCookie</c> on every
    /// authenticated request. MVC 5 re-issues the cookie each time to slide its expiry; cookie
    /// authentication does that itself with SlidingExpiration, which is set below.
    ///
    /// The login path is <c>Home/NotLoggedIn</c> and not the <c>loginUrl</c> in Web.config, which
    /// says <c>Home/Logon</c>. That is deliberate and it is what 4.8 does too: the site guards its
    /// pages with its own <c>[Auth]</c> attribute, and AppCode/Auth.cs sends an unauthenticated
    /// visitor to NotLoggedIn. FormsAuthentication's loginUrl is what would apply if nothing else
    /// did. Checked before matching it, because the two lines disagree and the config one is the
    /// more obvious to copy.
    ///
    /// The cookie is NOT named .ASPXAUTH. Sharing the name with the Framework site would invite
    /// the two to read each other's cookies, and the ticket formats have nothing in common - the
    /// failure would be a confusing 500 rather than a clean "not signed in".
    /// </summary>
    public static class ZkAuth
    {
        public const string Scheme = CookieAuthenticationDefaults.AuthenticationScheme;

        /// <summary>Registers cookie authentication. Call before Build().</summary>
        public static IServiceCollection AddZkAuthentication(this IServiceCollection services,
                                                             string loginPath = "/Home/NotLoggedIn")
        {
            services.AddAuthentication(Scheme)
                .AddCookie(Scheme, options =>
                {
                    options.Cookie.Name = "ZkAuth";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.LoginPath = loginPath;
                    // Web.config: <forms loginUrl="~/Home/Logon" timeout="2880" /> - 2880
                    // MINUTES, which is two days. This said 30 days, which is not a decision
                    // anyone recorded and is fifteen times as long for a stolen cookie to stay
                    // good. Spelled in minutes so it reads the same as the line it comes from.
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(2880);
                    options.SlidingExpiration = true;
                });
            return services;
        }

        /// <summary>
        /// Turns the signed-in name into an Account and publishes it where Global reads it.
        /// Must run after UseAuthentication, which is what populates HttpContext.User.
        /// </summary>
        public static IApplicationBuilder UseZkAccount(this IApplicationBuilder app) =>
            app.Use(async (context, next) =>
            {
                bool fromToken;
                var account = Resolve(context, out fromToken);

                if (account != null)
                {
                    var banned = FindSiteBan(account, context);
                    if (banned != null)
                    {
                        // Global.asax writes exactly these three lines and ends the response.
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "text/plain";
                        await context.Response.WriteAsync(
                            string.Format("You are banned! (IP match to account {0})\n", banned.AccountByAccountID?.Name)
                            + string.Format("Ban expires: {0}\n", banned.BanExpires)
                            + string.Format("Reason: {0}\n", banned.Reason));
                        return;
                    }

                    context.Items[Global.AccountItemKey] = account;

                    // The one SetAuthCookie that is NOT the sliding-expiry re-issue the class
                    // comment above explains away. A token is single use: SessionTokenStore.Redeem
                    // removes it whether or not it was valid. So the request that redeems one is
                    // the only chance to turn it into a session, and without this the player is
                    // signed in for exactly that request and anonymous on their next click - a
                    // page that renders their name above a menu that says they are logged out.
                    //
                    // Global.asax calls SetAuthCookie for both paths and the reasoning for
                    // dropping it covered only the cookie one, where there is already a cookie to
                    // slide. Here there is none to slide.
                    if (fromToken) await SignIn(context, account);
                }

                // Global.asax, at the end of PostAuthenticateRequest:
                //
                //     var removeCake = Regex.Replace(Request.Url.ToString(), ...);
                //     if (removeCake != Request.Url.ToString()) Response.Redirect(removeCake, true);
                //
                // The spent token comes back out of the address bar. It is in browser history and
                // in the Referer of every link the player follows off the site otherwise, and the
                // URL they copy to someone else carries it too. Done whether or not anything was
                // redeemed, which is what Global.asax does - the token is spent either way.
                //
                // Only the QUERY STRING, so a POST that carries the token as a form field is not
                // turned into a GET with no body. That is Global.asax's behaviour as well: it
                // matches on Request.Url, which does not contain the form.
                if (!string.IsNullOrEmpty(context.Request.Query[GlobalConst.SessionTokenVariable]))
                {
                    context.Response.Redirect(WithoutToken(context.Request));
                    return;
                }

                await next();
            });

        /// <summary>The request's own URL with the session token taken out of the query string.</summary>
        private static string WithoutToken(HttpRequest request)
        {
            var kept = request.Query
                              .Where(x => x.Key != GlobalConst.SessionTokenVariable)
                              .SelectMany(x => x.Value.Select(v => Uri.EscapeDataString(x.Key)
                                                                   + "=" + Uri.EscapeDataString(v)))
                              .ToList();

            return request.PathBase + request.Path
                   + (kept.Count > 0 ? "?" + string.Join("&", kept) : "");
        }

        private static Account Resolve(HttpContext context, out bool fromToken)
        {
            fromToken = false;
            var db = new ZkDataContext();

            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var name = context.User.Identity.Name;
                if (!string.IsNullOrEmpty(name)) return Account.AccountByName(db, name);
            }

            // A player arriving from the game client carries a one-use token instead of a cookie.
            var token = context.Request.Query[GlobalConst.SessionTokenVariable].FirstOrDefault();

            if (string.IsNullOrEmpty(token) && context.Request.HasFormContentType)
            {
                // EnableBuffering FIRST, and rewind after. Reading Request.Form consumes the body,
                // and this middleware runs on every request - so without this, any action that needs
                // the RAW body gets an empty stream, having done nothing wrong itself.
                //
                // That is not hypothetical: it is how ContributionsController's PayPal IPN handler
                // failed, silently. The fields parsed perfectly and the contribution was recorded,
                // while the bytes sent to PayPal for verification were empty, so every real payment
                // would have been stamped VERIFICATION FAILED. MVC 5 has no such problem - classic
                // ASP.NET buffers the entity body, so Request.Params and BinaryRead both work.
                //
                // Checked by CheckIpnBodyRead in ZeroKWeb.Host.
                context.Request.EnableBuffering();
                token = context.Request.Form[GlobalConst.SessionTokenVariable].FirstOrDefault();
                context.Request.Body.Position = 0;
            }
            // Request[key] on MVC 5 reads the query string, then the form, then the COOKIES -
            // and ZeroKLobby/BrowserInterop.cs:37 puts the token in a cookie as well as on the
            // URL, so the cookie is a path the live site really uses and this read only the
            // first two.
            if (string.IsNullOrEmpty(token))
                token = context.Request.Cookies[GlobalConst.SessionTokenVariable];

            if (!string.IsNullOrEmpty(token))
            {
                var accountID = Global.LobbyApi?.RedeemSessionToken(token);
                if (accountID != null)
                {
                    fromToken = true;
                    return db.Accounts.Find(accountID.Value);
                }
            }

            return null;
        }

        /// <summary>
        /// The site ban, looked up the way Global.asax looks it up - by account, address, and the
        /// user and install ids of the most recent login, so a ban follows a machine as well as a
        /// name.
        /// </summary>
        private static Punishment FindSiteBan(Account account, HttpContext context)
        {
            var address = context.Connection.RemoteIpAddress?.ToString();
            var lastLogin = account.AccountUserIDs.OrderByDescending(x => x.LastLogin).FirstOrDefault();
            return Punishment.GetActivePunishment(account.AccountID, address, lastLogin?.UserID,
                                                  lastLogin?.InstallID, x => x.BanSite);
        }

        /// <summary>Signs a verified account in. The cookie carries the name, nothing more.</summary>
        public static Task SignIn(HttpContext context, Account account)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, account.Name),
                new Claim(ClaimTypes.NameIdentifier, account.AccountID.ToString()),
            }, Scheme);

            return context.SignInAsync(Scheme, new ClaimsPrincipal(identity),
                                       new AuthenticationProperties { IsPersistent = true });
        }

        public static Task SignOut(HttpContext context) => context.SignOutAsync(Scheme);

        /// <summary>
        /// The site's own password check, which is <see cref="Account.AccountVerify"/> behind
        /// Zero-K.info/AppCode/AuthServiceClient.cs - a name that suggests a service call and is
        /// in fact a local BCrypt comparison. Null when the credentials do not match.
        /// </summary>
        public static Account Verify(string login, string password)
        {
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password)) return null;
            return Account.AccountVerify(new ZkDataContext(), login, Utils.HashLobbyPassword(password));
        }
    }
}
