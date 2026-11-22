using System.Linq;
using Microsoft.AspNetCore.Mvc;
using ZkData;

namespace ZeroKWeb.ViewComponents
{
    /// <summary>
    /// ASP.NET Core's replacement for <c>Html.RenderAction("Index", "Poll", new { pollID })</c>.
    ///
    /// **Seventh of seven**, which closes the set that ChildActionCompat has enumerated since it
    /// was written. Two callers: Shared/UserDetail.cshtml, and ForumParser/Tags/PollTag.cs - the
    /// one call site that is linked C# rather than a view, and therefore the one that will need a
    /// different answer, because a BBCode tag cannot invoke a view component.
    ///
    /// PollController is not linked, so the body is the action's rather than a call into it. It is
    /// two lines, which is why transcription is acceptable here and was not for
    /// MyCommanderProfile.
    ///
    /// <c>return null</c> in the original meant "render nothing" for a missing poll; a view
    /// component returns empty content for the same thing, since returning null from Invoke
    /// throws.
    /// </summary>
    public class PollViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(int pollID)
        {
            var db = new ZkDataContext();
            var poll = db.Polls.FirstOrDefault(x => x.PollID == pollID);
            if (poll == null) return Content("");
            return View("~/Views/Poll/PollView.cshtml", poll);
        }
    }
}
