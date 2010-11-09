using System.Linq;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{
	public class ForumController: Controller
	{
		public ActionResult SubmitPost(int threadID, string text)
		{
			if (!Global.IsAccountAuthorized) return Content("Not logged in");

			var db = new ZkDataContext();
			var thread = db.ForumThreads.Single(x => x.ForumThreadID == threadID);
			thread.ForumPosts.Add(new ForumPost() { AuthorAccountID = Global.AccountID, Text = text });
			db.SubmitChanges();
			return RedirectToAction("Detail", "Missions", new { id = thread.Missions.First().MissionID }); // todo finish properly
		}
	}
}