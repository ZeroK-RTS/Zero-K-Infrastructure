using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Collections.Generic;

namespace ZeroKWeb
{
    /// <summary>
    /// MVC 5's <c>AsyncController</c>, which ContentServiceController and MissionServiceController
    /// derive from.
    ///
    /// It existed because MVC 5 needed to be told that an action returning a Task should be awaited
    /// rather than treated as a result. ASP.NET Core awaits every action, so the distinction is
    /// gone - which makes an empty subclass of Controller the whole of the port, and the right
    /// shape: the two controllers keep saying what they meant, and nothing pretends to do work
    /// that no longer exists.
    /// </summary>
    public abstract class AsyncController : Controller
    {
        /// <summary>
        /// MVC 5's <c>Controller.TempDataProvider</c>. Both controllers set it to a
        /// NullTempDataProvider with the comment "this disable session state upkeep".
        ///
        /// ASP.NET Core has no such property - the provider is a DI service - so this accepts the
        /// assignment and keeps it. That is not a no-op dressed up: the problem being worked around
        /// does not exist here. MVC 5 tried to use TempData through session state and threw when
        /// sessions were off; ASP.NET Core's default provider is cookie-based and needs no session
        /// at all. Neither controller reads TempData, so there is nothing whose behaviour could
        /// differ - and it is a property rather than a discarded setter so that a future reader can
        /// see what was set.
        /// </summary>
        public ITempDataProvider TempDataProvider { get; set; }
    }

    /// <summary>
    /// The ASP.NET Core twin of Zero-K.info/AppCode/NullTempDataProvider.cs. Same name, different
    /// interface: MVC 5's ITempDataProvider takes a ControllerContext and Core's takes an
    /// HttpContext, so this is one of the cases where no shim can bridge the two and the file has
    /// to exist twice.
    /// </summary>
    public class NullTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context)
        {
            return new Dictionary<string, object>();
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
            // As in the original: this provider does not support temp data.
        }
    }
}
