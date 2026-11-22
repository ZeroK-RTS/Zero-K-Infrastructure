using System;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace ZeroKWeb.Compat
{
    /// <summary>
    /// MVC 5's <c>Request</c> and <c>Server</c>, as far as the views actually use them.
    ///
    /// Both were properties on the view page carrying members ASP.NET Core does not have -
    /// and two of them, <c>Request.Params[...]</c> and <c>Request.Url</c>, are an indexer and
    /// a property. C# has no extension properties or indexers, so no extension method can
    /// supply them however it is written; the only way to keep the views unedited is to hand
    /// them an object of a different type. Hence a wrapper rather than the raw HttpRequest.
    ///
    /// The surface is deliberately only what the views use, counted rather than guessed:
    /// Request.Url (3), Request.Params (1), Request.IsAjaxRequest (1), Server.MapPath (4),
    /// Server.HtmlEncode (1). Anything else should fail to compile rather than quietly
    /// resolve to something that behaves differently.
    /// </summary>
    public sealed class Mvc5Request
    {
        private readonly HttpRequest request;

        public Mvc5Request(HttpRequest request) => this.request = request;

        /// <summary>
        /// MVC 5's Params merged query string, form, cookies and server variables. The views
        /// use it once, for a query-string flag, so that is what this reads - broadening it
        /// silently would be worse than not having it.
        /// </summary>
        public string this[string key] => request?.Query[key];

        public ParamsAccessor Params => new ParamsAccessor(request);

        public Uri Url => request == null
            ? null
            : new Uri($"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}{request.QueryString}");

        // request.Headers[...] is StringValues, and the null-conditional makes it
        // StringValues?, whose == against a string is ambiguous (CS0034). Written wrong here
        // once already, in Mvc5PageCompat - and it survived because ZeroKWeb.Core cannot
        // report it: its views carry declaration errors, so no method body in that project is
        // bound, including this one. Only ZeroKWeb.Render and ZeroKWeb.Host, which contain
        // just the views that compile, ever see errors in the shims themselves.
        public bool IsAjaxRequest() => request != null && request.Headers["X-Requested-With"] == "XMLHttpRequest";

        public sealed class ParamsAccessor
        {
            private readonly HttpRequest request;
            public ParamsAccessor(HttpRequest request) => this.request = request;
            public string this[string key] => request?.Query[key];
        }
    }

    /// <summary>
    /// MVC 5's <c>Server</c>. MapPath resolved a ~/ path against the site root, which is
    /// IWebHostEnvironment.WebRootPath here. HtmlEncode was HttpUtility.HtmlEncode, whose
    /// counterpart is WebUtility.HtmlEncode - not ASP.NET Core's HtmlEncoder, which escapes
    /// far more (see HtmlHelperExtensions.Ported.cs).
    /// </summary>
    public sealed class Mvc5Server
    {
        private readonly IWebHostEnvironment environment;

        public Mvc5Server(IWebHostEnvironment environment) => this.environment = environment;

        public string MapPath(string virtualPath)
        {
            var relative = (virtualPath ?? string.Empty).TrimStart('~').TrimStart('/', '\\');
            var root = environment?.WebRootPath ?? Directory.GetCurrentDirectory();
            return Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        }

        /// <summary>
        /// Shared/UserDetail.cshtml calls this on a commander name, so its rules are part of
        /// what the site serves rather than an implementation detail.
        ///
        /// MVC 5's is HttpUtility.HtmlEncode. WebUtility.HtmlEncode is NOT obviously the same
        /// function - it is a different type in a different assembly - so the capture asks the
        /// real one and ZeroKWeb.Render compares:
        ///
        ///     a &amp;lt; b &amp;amp; c &amp;quot; d &amp;#39; e &amp;gt; f &amp;#252; &amp;#169; 中
        ///
        /// They agree, including the two parts worth naming: characters 160-255 become numeric
        /// entities, and characters ABOVE 255 are left alone, so a CJK name passes through
        /// untouched. Neither is what ASP.NET Core's own HtmlEncoder does, which is the encoder
        /// a port reaches for by default - it spells the apostrophe &amp;#x27; and escapes
        /// non-ASCII as &amp;#xNNNN;. That is the positive control for this check.
        /// </summary>
        public string HtmlEncode(string value) => WebUtility.HtmlEncode(value);
    }
}
