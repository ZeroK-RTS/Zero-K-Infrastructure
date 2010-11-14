using System;
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
			if (string.IsNullOrEmpty(text)) return Content("Please type some text :)");

			var db = new ZkDataContext();
			var thread = db.ForumThreads.Single(x => x.ForumThreadID == threadID);
			thread.ForumPosts.Add(new ForumPost() { AuthorAccountID = Global.AccountID, Text = text });
		  thread.LastPost = DateTime.UtcNow;
			db.SubmitChanges();
			return RedirectToAction("Detail", "Missions", new { id = thread.Missions.First().MissionID }); // todo finish properly
		}
	}
}