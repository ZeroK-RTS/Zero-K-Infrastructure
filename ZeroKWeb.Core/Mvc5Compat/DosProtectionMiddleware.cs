using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace ZeroKWeb.Compat
{
    /// <summary>
    /// The ASP.NET Core half of <see cref="DosProtector"/>. The counting is the SAME FILE -
    /// Zero-K.info/AppCode/DosProtector.Portable.cs, linked - so this cannot drift from what the
    /// live site does; only the three things that have no meaning off System.Web are written here.
    ///
    /// Global.asax runs it on PostMapRequestHandler and EndRequest:
    ///
    ///     if (protector.CanQuery(Request)) protector.RequestStart(Request);
    ///     else { Response.StatusCode = 429; Response.StatusDescription = "..."; Response.End(); }
    ///     ...
    ///     EndRequest += (s, e) => { protector.RequestEnd(Request); ... };
    ///
    /// Without this the port has NO rate limiting of any kind, which the live site does have, and
    /// which is a property nothing else in the harness would ever report on.
    ///
    /// **Where it is registered is the exemption.** Global.asax exempts a request IIS is serving
    /// with no managed handler, which is how a static file presents there. This middleware goes in
    /// AFTER UseStaticFiles, so a static file is answered before it is reached - same exemption,
    /// and no line of code that could be forgotten. The tooltip endpoint is still exempted by
    /// name, because it is not static and a page asks for it many times while a visitor moves the
    /// mouse.
    ///
    /// The try/finally has no counterpart in Global.asax and is not a change of behaviour: there
    /// EndRequest and the Error handler BOTH call RequestEnd, which is the same thing said twice
    /// because an exception otherwise leaves the request counted as in flight forever. One
    /// finally says it once.
    /// </summary>
    public static class DosProtectionMiddleware
    {
        public const string TooMany = "Too many requests from your IP address, please try again later";

        private static readonly DosProtector Protector = new DosProtector();

        /// <summary>The shared counter, so a check can ask what it has seen.</summary>
        public static DosProtector Instance => Protector;

        public static IApplicationBuilder UseDosProtection(this IApplicationBuilder app) =>
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.Value != null
                    && context.Request.Path.Value.Contains("/Home/GetTooltip"))
                {
                    await next();
                    return;
                }

                var ip = context.Connection.RemoteIpAddress?.ToString();

                if (!Protector.CanQuery(ip))
                {
                    // 429, with the same sentence. Global.asax puts it in the status DESCRIPTION,
                    // which HTTP/2 has no room for and Kestrel does not send, so it is written as
                    // the body instead - a client reading the reason gets it either way, and one
                    // reading only the code sees the same 429.
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync(TooMany);
                    return;
                }

                var requestID = Protector.RequestStart(ip);
                try
                {
                    await next();
                }
                finally
                {
                    Protector.RequestEnd(requestID);
                }
            });
    }
}
