using Microsoft.AspNetCore.Mvc;
using ZeroKWeb.Controllers;
using ZkData;

namespace ZeroKWeb.ViewComponents
{
    /// <summary>
    /// ASP.NET Core's replacement for <c>Html.RenderAction("CommanderProfile", ...)</c> in
    /// My/Commanders.cshtml.
    ///
    /// Sixth of seven, and the third and last that carries <c>[Auth]</c>. As with
    /// PlanetwarsMatchMaker and LobbyChatNotification, a view component inherits nothing from the
    /// action's filter pipeline, so the check is written out - and as with those, this is the
    /// hazard ChildActionCompat has refused to paper over from the beginning.
    ///
    /// **It does not reproduce the action.** MyController.CommanderProfile is a POST handler that
    /// deletes commanders, changes chassis and saves modules inside a TransactionScope; what the
    /// view calls it for is the READ that follows. So this calls
    /// <see cref="MyController.BuildCommanderProfileModel"/>, which was extracted from
    /// GetCommanderProfileView for exactly this - twenty lines of unlock filtering that would
    /// otherwise have been transcribed, and transcription has put two defects into ten lines of
    /// helper in this port already.
    ///
    /// The mutating half stays in the controller, where a POST reaches it. A view component
    /// rendering inside a GET is the wrong place to delete a commander.
    /// </summary>
    public class MyCommanderProfileViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(int profileNumber)
        {
            // [Auth], made explicit.
            if (Global.Account == null) return Content("");

            // The action's own guard, kept: the profile number comes from a loop in the view, but
            // a component can be invoked with anything.
            if (profileNumber < 1 || profileNumber > GlobalConst.CommanderProfileCount)
                return Content("WTF! get lost");

            var db = new ZkDataContext();
            return View("~/Views/My/CommanderProfile.cshtml",
                        MyController.BuildCommanderProfileModel(db, profileNumber));
        }
    }
}
