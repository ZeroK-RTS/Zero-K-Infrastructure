using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace ZeroKWeb
{
    /// <summary>
    /// The ASP.NET Core half of two twins in Zero-K.info/AppCode/ControllerCompat.cs. Same method
    /// names, so the linked controllers compile against either framework.
    ///
    /// Both are here rather than at the call site because C# has no extension properties, which is
    /// the same wall Server.MapPath and Request.Params ran into. <c>Request.InputStream</c> and
    /// <c>Request.Form.AllKeys</c> are properties in MVC 5, and a member always beats a same-named
    /// extension method - so the twins need names of their own and a production edit.
    /// </summary>
    public static class RequestBodyCompat
    {
        /// <summary>
        /// MVC 5's <c>Request.InputStream</c>: the raw request body, rewound and readable.
        /// GithubController needs the exact bytes, because it HMACs them against X-Hub-Signature.
        ///
        /// It returns a MemoryStream rather than Request.Body, and that is not tidiness. Kestrel
        /// disallows synchronous reads unless AllowSynchronousIO is set, which this port does not
        /// set - so <c>Request.Body.CopyTo(...)</c>, the shape of the MVC 5 call site, would throw
        /// at runtime while compiling perfectly. Copying asynchronously and blocking here keeps
        /// the call site unedited and the failure impossible. Request.Body is also read-once and
        /// not seekable, and a caller that reads it twice would silently get nothing the second
        /// time; a MemoryStream cannot do that either.
        ///
        /// The block is deliberate and affordable: the one caller is a webhook. It is the same
        /// trade RemoteLobbyServerApi makes for its synchronous members.
        /// </summary>
        public static Stream RequestInputStream(this ControllerBase controller)
        {
            var body = new MemoryStream();
            controller.Request.Body.CopyToAsync(body).GetAwaiter().GetResult();
            body.Position = 0;
            return body;
        }

        /// <summary>
        /// MVC 5's <c>Request.Form.AllKeys</c>. Core's IFormCollection spells it Keys, and PollController
        /// reads it to find which radio button was posted.
        /// </summary>
        public static IEnumerable<string> AllKeysCompat(this IFormCollection form) => form.Keys;
    
        /// <summary>
        /// MVC 5's <c>DependencyResolver.Current.GetService(typeof(T))</c>, which PostHistoryController
        /// uses to borrow ForumController and call SubmitPost on it directly.
        ///
        /// ActivatorUtilities rather than GetService because ASP.NET Core does not register controllers
        /// in DI unless asked to, and a null here would surface as a NullReferenceException far from
        /// its cause.
        ///
        /// The borrowed controller is given THIS request's context, and that is the whole reason this
        /// is a named twin rather than one line at the call site: SubmitPost reads
        /// Request.UserHostAddressCompat(), so a controller with no context cannot run it.
        /// </summary>
        public static T BorrowController<T>(this ControllerBase caller) where T : ControllerBase
        {
            var borrowed = ActivatorUtilities.CreateInstance<T>(caller.HttpContext.RequestServices);
            borrowed.ControllerContext = caller.ControllerContext;
            return borrowed;
        }

        /// <summary>
        /// MVC 5's <c>Request.Params</c> and <c>Request.BinaryRead(Request.ContentLength)</c>, which
        /// ContributionsController's PayPal IPN handler passes to ImportIpnPayment in one call.
        ///
        /// **They have to be one method here, and that is the whole point.** IPN verification posts
        /// the received bytes straight back to PayPal, so the parsed fields and the raw bytes must
        /// describe the same request. In ASP.NET Core reading the form consumes the body, and C#
        /// evaluates arguments left to right - so the MVC 5 call site, translated literally, would
        /// parse the form and then hand PayPal an EMPTY body to verify. It would compile, the
        /// contribution would be recorded, and every payment would be flagged VERIFICATION FAILED.
        ///
        /// So the body is buffered, read once as bytes, rewound, and only then parsed as a form.
        ///
        /// Query string and form only. MVC 5's Params also merged cookies and server variables, and
        /// leaving them out is deliberate: PayPal sends every IPN field in the body, and a merge that
        /// lets a caller's own cookie supply one is a worse thing to be faithful to.
        /// </summary>
        public static NameValueCollection ReadIpnRequest(this ControllerBase controller, out byte[] raw)
        {
            var request = controller.Request;

            // Only if nobody has buffered it already - ZkAuth's middleware does, for exactly this
            // reason, and wrapping a buffered stream a second time would be the thing to avoid.
            if (!request.Body.CanSeek) request.EnableBuffering();
            request.Body.Position = 0;
            var buffer = new MemoryStream();
            request.Body.CopyToAsync(buffer).GetAwaiter().GetResult();
            raw = buffer.ToArray();
            request.Body.Position = 0;

            var values = new NameValueCollection();
            foreach (var pair in request.Query) values.Add(pair.Key, pair.Value.ToString());
            if (request.HasFormContentType)
                foreach (var pair in request.Form) values.Add(pair.Key, pair.Value.ToString());
            return values;
        }
}
}
