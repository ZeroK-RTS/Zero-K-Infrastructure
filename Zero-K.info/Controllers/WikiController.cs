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
            var post =
                new ZkDataContext().ForumCategories.First(x=>x.IsWiki).ForumThreads.FirstOrDefault(x => x.Title == node)?.ForumPosts.OrderBy(x => x.ForumPostID).FirstOrDefault();

            return View(post);
        }
    }
}