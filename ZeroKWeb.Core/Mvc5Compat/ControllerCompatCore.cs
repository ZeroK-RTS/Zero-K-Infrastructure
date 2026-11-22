using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ZeroKWeb.Compat;

namespace ZeroKWeb
{
    /// <summary>
    /// The ASP.NET Core twin of Zero-K.info/AppCode/ControllerCompat.cs. Same method name, so the
    /// linked controllers compile against either framework.
    /// </summary>
    public static class ControllerCompat
    {
        public static string MapPath(this Controller controller, string virtualPath)
        {
            var environment = controller?.HttpContext?.RequestServices
                ?.GetService(typeof(IWebHostEnvironment)) as IWebHostEnvironment;
            return new Mvc5Server(environment).MapPath(virtualPath);
        }
    }

    public static class NotFoundCompat
    {
        /// <summary>
        /// MVC 5's <c>Controller.HttpNotFound()</c>. ASP.NET Core spells the same thing
        /// <c>NotFound()</c>, so a linked controller that says HttpNotFound finds this.
        ///
        /// An extension rather than a member, and that is what makes it safe: ASP.NET Core's
        /// Controller has no instance method of this name, so the extension applies here and is
        /// not compiled at all for MVC 5, where the real instance method wins.
        /// </summary>
        public static ActionResult HttpNotFound(this Controller controller) => controller.NotFound();
    }

    public static class HttpResponseCompat
    {
        /// <summary>
        /// MVC 5's <c>Response.ClearContent()</c>, which discarded anything already written to the
        /// response buffer. NewsController calls it before writing an RSS feed.
        ///
        /// A genuine no-op here rather than a stub: at that point in an ASP.NET Core action
        /// nothing has been written, the response has not started, and there is nothing to clear.
        /// Calling it after the response HAS started is the case MVC 5 handled and this cannot -
        /// so that throws rather than pretending, because a silently unflushed buffer is exactly
        /// the failure this port keeps meeting.
        /// </summary>
        public static void ClearContent(this HttpResponse response)
        {
            if (response != null && response.HasStarted)
                throw new System.NotSupportedException(
                    "Response.ClearContent() after the response has started is not possible on "
                    + "ASP.NET Core - the bytes are already on the wire. See "
                    + "ZeroKWeb.Core/Mvc5Compat/ControllerCompatCore.cs.");
        }
    }
}
