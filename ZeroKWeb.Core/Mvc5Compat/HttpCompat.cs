using System.Linq;
using Microsoft.AspNetCore.Http;

namespace ZeroKWeb.Compat
{
    /// <summary>
    /// Two request/response members MVC 5 had and ASP.NET Core does not, both used by linked
    /// controllers and both mechanical.
    ///
    /// Response.Write and Response.Flush wrote to a synchronous output stream. ASP.NET Core's
    /// response is asynchronous, and these block on it - which is exactly what the original
    /// code does, because it was written against a blocking API. Faithful, and not what a
    /// controller written for this framework would do.
    /// </summary>
    public static class HttpCompat
    {
        public static void Write(this HttpResponse response, string text)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(text);
            response.Body.Write(bytes, 0, bytes.Length);
        }

        public static void Flush(this HttpResponse response) => response.Body.Flush();

        /// <summary>
        /// The twin of Zero-K.info/AppCode/HttpCompat.cs. MVC 5 had Request.UserHostAddress;
        /// ASP.NET Core keeps the client address on the connection.
        /// </summary>
        public static string UserHostAddressCompat(this HttpRequest request)
            => request?.HttpContext?.Connection?.RemoteIpAddress?.ToString();

        // AllKeys is deliberately absent. MyController uses Request.Form.AllKeys as a
        // PROPERTY, and C# has no extension properties - the fifth time that has decided
        // something in this port. It is fixed in the controller instead, which unlike a view
        // can be verified here: tools/build-website.sh compiles the Framework C#.
        //
        // Request.Form.Keys.Cast<string>() compiles on both. MVC 5's Form is a
        // NameValueCollection whose Keys is non-generic, so the Cast is required there and
        // harmless here.
    }
}
