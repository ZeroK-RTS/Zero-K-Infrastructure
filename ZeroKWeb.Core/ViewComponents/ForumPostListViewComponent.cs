using System.Linq;
using Microsoft.AspNetCore.Mvc;
using ZeroKWeb.Controllers;
using ZkData;

namespace ZeroKWeb.ViewComponents
{
    /// <summary>
    /// ASP.NET Core's replacement for <c>@Html.Action("GetPostList", "Forum", ...)</c>.
    ///
    /// The first of seven. See PortedViews/README.md for why the call site cannot be shared
    /// between the two builds, and ZeroKWeb.Core/Mvc5Compat/ChildActionCompat.cs for the shims
    /// that keep the other six compiling in the meantime.
    ///
    /// The body is <see cref="ForumController.GetPostList"/>'s, with two differences that are
    /// forced rather than chosen:
    ///
    /// - It builds the model itself instead of receiving one from the model binder. A view
    ///   component is invoked with an anonymous object, not through model binding, so the
    ///   optional parameters are ordinary C# defaults here.
    /// - It returns the partial by full path. A view component resolves views under
    ///   Views/Shared/Components/&lt;Name&gt;/ by convention, and PostList.cshtml is not there -
    ///   it is a real view the Forum controller also renders on its own, so it stays put.
    ///
    /// GetPostList carries no filters, which is why this one is a straight translation. Three
    /// of the seven carry [Auth], and those cannot be: a view component inherits nothing from
    /// the action's filter pipeline, so each will have to perform its check explicitly and that
    /// check is the part to get right, not the query.
    ///
    /// **This compiles and is not yet renderable.** Forum/PostList.cshtml does not compile on
    /// .NET 9 (CS0103, CS0117, CS0246), so nothing can exercise this end to end until it does.
    /// That is tracked in the view inventory, not here.
    /// </summary>
    public class ForumPostListViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(int threadID, bool disablePostComment = false, int goToPost = 0,
                                           string search = null, string user = null)
        {
            var db = new ZkDataContext();
            var model = new ForumController.PostListModel
            {
                ThreadID = threadID,
                DisablePostComment = disablePostComment,
                GoToPost = goToPost,
                Search = search,
                User = user,
            };

            var thread = db.ForumThreads.First(x => x.ForumThreadID == model.ThreadID &&
                                                    (x.RestrictedClanID == null || x.RestrictedClanID == Global.ClanID));

            var posts = thread.ForumPosts.AsQueryable();

            if (!string.IsNullOrEmpty(model.Search))
            {
                posts = Global.ForumPostIndexer.FilterPosts(posts, model.Search);
            }
            if (!string.IsNullOrEmpty(model.User))
            {
                var filterAccountID = (db.Accounts.FirstOrDefault(x => x.Name == model.User) ??
                                       db.Accounts.FirstOrDefault(x => x.Name.Contains(model.User)))?.AccountID;
                if (filterAccountID.HasValue) posts = posts.Where(x => x.AuthorAccountID == filterAccountID);
            }

            model.Data = posts.OrderBy(x => x.ForumPostID);
            model.Thread = thread;

            if (!model.DisablePostComment)
            {
                var mode = thread.ForumCategory.ForumMode;
                model.DisablePostComment = thread.IsLocked || mode == ForumMode.Maps || mode == ForumMode.Missions ||
                                           mode == ForumMode.SpringBattles || mode == ForumMode.Clans ||
                                           mode == ForumMode.Planets || mode == ForumMode.GameModes;
            }

            return View("~/Views/Forum/PostList.cshtml", model);
        }
    }
}
