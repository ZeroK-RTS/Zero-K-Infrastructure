using Microsoft.AspNetCore.Mvc;

namespace ZeroKWeb.ViewComponents
{
    /// <summary>
    /// ASP.NET Core's replacement for <c>@Html.Action("MatchMaker")</c> in Galaxy.cshtml.
    ///
    /// **The first of the seven that carries [Auth]**, and therefore the first where the check
    /// has to be written out. A view component inherits nothing from an action's filter
    /// pipeline: it is invoked from inside a view that has already been authorized for its own
    /// reasons, and no filter runs on the way in. This is exactly the hazard
    /// Mvc5Compat/ChildActionCompat.cs refuses to paper over - a shim that invoked the action
    /// directly would have rendered this to an anonymous visitor with no check at all.
    ///
    /// The check is the same one AuthCompat applies: <c>Global.Account == null</c>. MatchMaker's
    /// [Auth] names no Role, so there is no second test.
    ///
    /// **Where this differs from MVC 5, deliberately.** The original [Auth] fails the whole
    /// REQUEST - it redirects to Home/NotLoggedIn with a ReturnUrl. A view component cannot do
    /// that sensibly: it is rendering a fragment inside someone else's page, and Galaxy.cshtml
    /// renders the matchmaker inline among other content. So an unauthorized viewer gets empty
    /// content and the rest of the page. That is a behaviour change, chosen rather than
    /// stumbled into, and it is the safe direction: it shows less than MVC 5 would, never more.
    ///
    /// **What is verified and what is not.** ZeroKWeb.Render checks the DENY path - that an
    /// anonymous viewer gets empty content, and that the component returns before touching
    /// Global.LobbyApi, which is null in every harness. The permit path cannot be checked here:
    /// it needs both an authenticated account, which waits on authentication middleware, and a
    /// running lobby server, which no harness has. So this component is verified to refuse and
    /// unverified to allow.
    /// </summary>
    public class PlanetwarsMatchMakerViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            // [Auth], made explicit. Before anything else, and in particular before
            // Global.LobbyApi, which is null when no lobby server is attached.
            if (Global.Account == null) return Content("");

            if (Global.LobbyApi.IsPlanetWarsMatchMakerRunning)
            {
                // admin view gets a per-viewer command so per-option flags render correctly
                var state = Global.LobbyApi.GeneratePlanetWarsLobbyCommand(Global.Account?.Name,
                                                                          Global.Account?.Faction?.Shortcut);
                if (state != null) return View("~/Views/Planetwars/PwMatchMaker.cshtml", state);
            }
            return Content("Match maker offline");
        }
    }
}
