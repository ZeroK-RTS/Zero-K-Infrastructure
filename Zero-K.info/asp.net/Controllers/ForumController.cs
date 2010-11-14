using System;
using System.Linq;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{
	public class ForumController: Controller
	{
		public ActionResult SubmitPost(int? threadID, int? resourceID, int? missionID, string text)
		{
			if (!Global.IsAccountAuthorized) return Content("Not logged in");
			if (string.IsNullOrEmpty(text)) return Content("Please type some text :)");
      
			var db = new ZkDataContext();
			var thread = db.ForumThreads.SingleOrDefault(x => x.ForumThreadID == threadID);
      if (thread == null && resourceID != null) // non existing thread, we posted new post on map
      {
        var res = db.Resources.Single(x => x.ResourceID == resourceID);
        thread = new ForumThread() { Title = res.InternalName };
        res.ForumThread = thread;
        db.ForumThreads.InsertOnSubmit(thread);
      }

			thread.ForumPosts.Add(new ForumPost() { AuthorAccountID = Global.AccountID, Text = text });
		  thread.LastPost = DateTime.UtcNow;
			db.SubmitChanges();

      if (thread.Missions != null) return RedirectToAction("Detail", "Missions", new { id = thread.Missions.MissionID }); 
      else return RedirectToAction("Detail", "Maps", new { id = thread.Resources.ResourceID });
		}
	}
}