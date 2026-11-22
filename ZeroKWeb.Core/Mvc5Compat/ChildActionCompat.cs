using System;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

// In System.Web.Mvc rather than ZeroKWeb.Compat: the ForumParser calls Html.Action from
// files that import the MVC 5 namespace and know nothing about this port's own.

namespace System.Web.Mvc
{
    /// <summary>
    /// MVC 5's child actions, which ASP.NET Core removed outright. 14 call sites - 13 in views
    /// and one in ForumParser/Tags/PollTag.cs, which is linked C# rather than a view - reach
    /// seven distinct actions:
    ///
    ///   Planetwars/Events        6 sites
    ///   Forum/GetPostList        3
    ///   Poll/Index               2   (one of them PollTag.cs)
    ///   Planetwars/Ladder        1
    ///   Planetwars/MatchMaker    1   [Auth]
    ///   Lobby/ChatNotification   1   [Auth]
    ///   My/CommanderProfile      1   [Auth]
    ///
    /// View components are the replacement, and they are a different shape - a class, a
    /// Views/Shared/Components folder, a changed call site. The rewrites do not compile on
    /// MVC 5, so they cannot be shared between the two builds, which is this port's whole
    /// technique. That is the real problem here, and it is not solved by these shims.
    ///
    /// These exist so the views COMPILE while that is still true, and they throw if anything
    /// actually reaches them. That is the point rather than a shortcoming.
    ///
    /// The alternative - invoking the controller action and rendering its result - would skip
    /// the filter pipeline. THREE of the seven carry [Auth], not one as this note previously
    /// said: MatchMaker, ChatNotification and CommanderProfile. A shim that silently rendered
    /// an authorization-protected action to an anonymous visitor is the exact failure this
    /// port has already produced three times in measurement and must not produce in behaviour.
    ///
    /// The overload set below covers every call site in the repository, which it did not
    /// before: Action(string), Action(string, object), RenderAction(string, object) and
    /// RenderAction(string, string, object) were missing, and four views were reporting
    /// CS1929, CS1503 or CS1501 for them. Those are not child-action findings, they are gaps
    /// in this file, and they were hiding what those views are really blocked on.
    ///
    /// A view that compiles only because of this file is NOT ready. tools/view-port-report.sh
    /// puts those in a `child-action` bucket rather than in `compiles` for exactly that reason.
    ///
    /// TopMenu.cshtml guards its call with Global.IsAccountAuthorized, so an anonymous
    /// request never reaches it, which is why the site layout can render at all before this
    /// is resolved.
    /// </summary>
    public static class ChildActionCompat
    {
        // Overloads that name a controller.

        public static void RenderAction(this IHtmlHelper helper, string action, string controller)
            => throw Unsupported(action, controller);

        public static void RenderAction(this IHtmlHelper helper, string action, string controller, object routeValues)
            => throw Unsupported(action, controller);

        public static IHtmlContent Action(this IHtmlHelper helper, string action, string controller)
            => throw Unsupported(action, controller);

        public static IHtmlContent Action(this IHtmlHelper helper, string action, string controller, object routeValues)
            => throw Unsupported(action, controller);

        // Overloads that do not: MVC 5 resolves the action on the CURRENT controller. Galaxy,
        // Planet, NewPost, Thread and Commanders all call these, and without them those views
        // failed to bind for a reason that had nothing to do with child actions.
        //
        // Action(string, object) and Action(string, string) coexist without ambiguity: a call
        // with two string arguments picks the second, because string is more specific than
        // object, which is the same rule MVC 5 applied.

        public static void RenderAction(this IHtmlHelper helper, string action)
            => throw Unsupported(action, "(current)");

        public static void RenderAction(this IHtmlHelper helper, string action, object routeValues)
            => throw Unsupported(action, "(current)");

        public static IHtmlContent Action(this IHtmlHelper helper, string action)
            => throw Unsupported(action, "(current)");

        public static IHtmlContent Action(this IHtmlHelper helper, string action, object routeValues)
            => throw Unsupported(action, "(current)");

        private static Exception Unsupported(string action, string controller) =>
            new NotSupportedException(
                $"Child action {controller}/{action} was invoked. ASP.NET Core removed child actions; " +
                "this call site needs rewriting as a view component. It is not shimmed on purpose - " +
                "invoking the action directly would skip its filters, and three of the seven actions " +
                "reached this way carry [Auth]: Planetwars/MatchMaker, Lobby/ChatNotification and " +
                "My/CommanderProfile. See ZeroKWeb.Core/Mvc5Compat/ChildActionCompat.cs.");
    }
}
