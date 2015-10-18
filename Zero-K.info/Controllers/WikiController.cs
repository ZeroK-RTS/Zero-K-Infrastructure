using System.Linq;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class WikiController: Controller
    {
        //
        // GET: /Wiki/
        public ActionResult Index(string node, string language = "") {
            var db = new ZkDataContext();
            var post = db.ForumThreads.FirstOrDefault(x => x.WikiKey == node)?.ForumPosts.OrderBy(x => x.ForumPostID).FirstOrDefault();
            if (post == null)
            {
                return RedirectToAction("NewPost","Forum",new {categoryID=db.ForumCategories.Where(x=>x.ForumMode == ForumMode.Wiki && !x.IsLocked).OrderBy(x=>x.SortOrder).FirstOrDefault()?.ForumCategoryID, wikiKey = node});
            } else return View("WikiIndex",post);
        }
    }
}