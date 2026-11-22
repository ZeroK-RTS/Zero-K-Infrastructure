using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ZeroKWeb.Compat
{
    /// <summary>
    /// The MVC 5 view-page surface that ASP.NET Core does not have, supplied to the views
    /// rather than edited out of them.
    ///
    /// 43 of Zero-K's views write <c>Page.Title = "..."</c> and the layout reads it back.
    /// <c>Page</c> is System.Web.WebPages' dynamic per-page bag; ASP.NET Core's equivalent is
    /// <c>ViewBag</c>, which flows from view to layout in exactly the same way. Handing the
    /// views a <c>Page</c> that *is* <c>ViewBag</c> means 43 files need no edit at all, and
    /// the MVC 5 build keeps working because nothing in it changed.
    ///
    /// Set as the base for every view by @inherits in _ViewImports.cshtml.
    /// </summary>
    public abstract class ZkRazorPage<TModel> : RazorPage<TModel>
    {
        public dynamic Page => ViewBag;

        /// <summary>
        /// MVC 5 put Request and Server straight on the view page. These are the MVC 5
        /// shapes, not ASP.NET Core's - see Mvc5RequestCompat.cs for why a wrapper and not
        /// the real HttpRequest.
        /// </summary>
        public Mvc5Request Request => new Mvc5Request(Context.Request);

        /// <summary>
        /// MVC 5's Ajax helper. 18 views use it; see Mvc5Compat/AjaxCompat.cs.
        ///
        /// The url helper comes from THIS page's ViewContext rather than from Global.UrlHelper(),
        /// which builds one from the ambient request. Two reasons, and the second is the one that
        /// bit: a view already has the ActionContext its links should resolve against, so going
        /// through the ambient is a longer way round to the same answer; and Global.UrlHelper()
        /// returns null when nothing has called Global.Configure - which is every harness - so
        /// the first component that rendered an Ajax form died on an ArgumentNullException from
        /// inside UrlHelperExtensions rather than doing anything useful.
        /// </summary>
        public Mvc5AjaxHelper Ajax => new Mvc5AjaxHelper(
            (Context?.RequestServices?.GetService(typeof(Microsoft.AspNetCore.Mvc.Routing.IUrlHelperFactory))
                as Microsoft.AspNetCore.Mvc.Routing.IUrlHelperFactory)?.GetUrlHelper(ViewContext),
            Output);

        public Mvc5Server Server => new Mvc5Server(
            Context.RequestServices.GetService(typeof(Microsoft.AspNetCore.Hosting.IWebHostEnvironment))
                as Microsoft.AspNetCore.Hosting.IWebHostEnvironment);
    }

    public static class Mvc5RequestCompat
    {
        /// <summary>
        /// MVC 5's Request.IsAjaxRequest(). ASP.NET Core dropped it but not the convention it
        /// reads - jQuery and the site's own scripts still send the header.
        /// </summary>
        public static bool IsAjaxRequest(this HttpRequest request)
            => request != null && request.Headers["X-Requested-With"] == "XMLHttpRequest";

        /// <summary>
        /// MVC 5's ViewContext.IsChildAction, which _ViewStart.cshtml uses to skip the layout
        /// when a view is rendered inside another.
        ///
        /// ASP.NET Core removed child actions, and with them the reason for the question:
        /// _ViewStart does not run for partials or view components at all, so a view reaching
        /// this code is never a child. Always false is the faithful answer, not a stub - but
        /// it is a judgement about behaviour rather than a translation, so it is written here
        /// where it can be argued with rather than buried in a view.
        ///
        /// **Why the name has Compat on the end.** In MVC 5 this is a PROPERTY, and C# has no
        /// extension properties - the wall Server.MapPath, Request.Params and Request.Url all
        /// hit. The usual answer, an extension method of the same name, does not work here
        /// either: a member always beats an extension method, so `ViewContext.IsChildAction()`
        /// on MVC 5 would try to invoke a bool. Hence a name neither stack already has, with a
        /// twin in Zero-K.info/AppCode/ViewContextCompat.cs that forwards to the real property.
        /// </summary>
        public static bool IsChildActionCompat(this ViewContext context) => false;
    }
}
