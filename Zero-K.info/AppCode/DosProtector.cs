using System.Web;

namespace ZeroKWeb
{
    /// <summary>
    /// The MVC 5 half of the rate limiter: everything that touches System.Web, and nothing else.
    ///
    /// The counting moved to DosProtector.Portable.cs unchanged, so that the .NET 9 host can
    /// compile the same file rather than carry a second copy of it. What is left here is pulling
    /// an address out of an HttpRequest, deciding what is exempt, and keeping the request id where
    /// EndRequest can find it - three things that have no meaning off System.Web and a different
    /// answer each on ASP.NET Core.
    /// </summary>
    public partial class DosProtector
    {
        /// <summary>
        /// Exempt: anything IIS is serving without a managed handler - which is how static files
        /// present here - and the tooltip endpoint, which a page calls many times while a visitor
        /// moves the mouse and which is cheap.
        ///
        /// On ASP.NET Core the first of these is positional instead: the middleware is registered
        /// after UseStaticFiles, so a static file is answered before it is reached. Same exemption,
        /// no equivalent line.
        /// </summary>
        public bool CanQuery(HttpRequest request)
        {
            if (request.RequestContext.HttpContext.Handler == null) return true;
            if (request.Path.Contains("/Home/GetTooltip")) return true;

            return CanQuery(request.UserHostAddress);
        }

        public void RequestStart(HttpRequest request)
        {
            request.RequestContext.HttpContext.Items["requestID"] = RequestStart(request.UserHostAddress);
        }

        public void RequestEnd(HttpRequest request)
        {
            RequestEnd(request.RequestContext.HttpContext.Items["requestID"] as long?);
        }
    }
}
