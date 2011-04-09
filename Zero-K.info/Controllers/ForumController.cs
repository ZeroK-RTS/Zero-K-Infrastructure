using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{
	public class ForumController: Controller
	{
		int PageSize = 100;

		[Authorize(Roles = "admin")]
		public ActionResult DeletePost(int? postID)
		{
			var db = new ZkDataContext();
			var thread = db.ForumPosts.Single(x => x.ForumPostID == postID);
			db.ForumPosts.DeleteOnSubmit(thread);
			db.SubmitChanges();
			return RedirectToAction("Thread", new { id = thread.ForumThreadID });
		}

		public ActionResult Index(int? categoryID)
		{
			var db = new ZkDataContext();
			var res = new IndexResult();

			res.Categories = db.ForumCategories.Where(x => Equals(x.ParentForumCategoryID, categoryID)).OrderBy(x => x.SortOrder);

			res.Path = GetCategoryPath(categoryID, db);
			res.CurrentCategory = res.Path.LastOrDefault();

			//if (res.CurrentCategory != null && res.CurrentCategory.IsMissions) res.Threads = db.ForumThreads.Where(x => Equals(x.ForumCategoryID, categoryID) && !Global.IsLimitedMode || x.Missions.ModRapidTag.StartsWith("zk:")).OrderByDescending(x => x.LastPost);
			//else
			res.Threads = db.ForumThreads.Where(x => Equals(x.ForumCategoryID, categoryID)).OrderByDescending(x => x.LastPost);

			return View(res);
		}

		public ActionResult NewPost(int? categoryID, int? threadID)
		{
			var res = new NewPostResult();
			var db = new ZkDataContext();

			if (threadID.HasValue)
			{
				var t = db.ForumThreads.Single(x => x.ForumThreadID == threadID.Value);
				res.CurrentThread = t;
				res.LastPosts = res.CurrentThread.ForumPosts.OrderByDescending(x => x.ForumPostID).Take(20);
				if (!categoryID.HasValue) categoryID = t.ForumCategoryID;
			}

			res.Path = GetCategoryPath(categoryID, db);
			res.CurrentCategory = res.Path.LastOrDefault();

			return View(res);
		}


		public ActionResult SubmitPost(int? threadID, int? categoryID, int? resourceID, int? missionID, int? springBattleID, string text, string title)
		{
			if (!Global.IsAccountAuthorized) return Content("Not logged in");
			if (string.IsNullOrEmpty(text)) return Content("Please type some text :)");

			var db = new ZkDataContext();
			var thread = db.ForumThreads.SingleOrDefault(x => x.ForumThreadID == threadID);

			if (threadID == null && categoryID.HasValue) // new thread
			{
				var cat = db.ForumCategories.Single(x => x.ForumCategoryID == categoryID.Value);
				if (cat.IsLocked) return Content("Thread is locked");

				if (string.IsNullOrEmpty(title)) return Content("Title cannot be empty");
				thread = new ForumThread();
				thread.CreatedAccountID = Global.AccountID;
				thread.Title = title;
				thread.ForumCategoryID = cat.ForumCategoryID;
				db.ForumThreads.InsertOnSubmit(thread);
			}

			if (thread == null && resourceID != null) // non existing thread, we posted new post on map
			{
				var res = db.Resources.Single(x => x.ResourceID == resourceID);
				thread = new ForumThread() { Title = res.InternalName, CreatedAccountID = Global.AccountID, LastPostAccountID = Global.AccountID };
				thread.ForumCategory = db.ForumCategories.FirstOrDefault(x => x.IsMaps);
				res.ForumThread = thread;
				db.ForumThreads.InsertOnSubmit(thread);
			}

			if (thread == null && springBattleID != null) // non existing thread, we posted new post on battle
			{
				var bat = db.SpringBattles.Single(x => x.SpringBattleID == springBattleID);
				thread = new ForumThread() { Title = bat.FullTitle, CreatedAccountID = Global.AccountID, LastPostAccountID = Global.AccountID };
				thread.ForumCategory = db.ForumCategories.FirstOrDefault(x => x.IsSpringBattles);
				bat.ForumThread = thread;
				db.ForumThreads.InsertOnSubmit(thread);
			}

			if (thread == null) return Content("Thread not found");
			if (thread.IsLocked) return Content("Thread is locked");

			var lastPost = thread.ForumPosts.OrderByDescending(x => x.ForumPostID).FirstOrDefault();

			if (lastPost == null || lastPost.AuthorAccountID != Global.AccountID || lastPost.Text != text)
			{
				//double post preventer
				thread.ForumPosts.Add(new ForumPost() { AuthorAccountID = Global.AccountID, Text = text });
				thread.LastPost = DateTime.UtcNow;
				thread.LastPostAccountID = Global.AccountID;
				thread.PostCount = thread.ForumPosts.Count();
				thread.UpdateLastRead(Global.AccountID, true, thread.LastPost);

				db.SubmitChanges();
			}

			if (missionID.HasValue) return RedirectToAction("Detail", "Missions", new { id = missionID });
			else if (resourceID.HasValue) return RedirectToAction("Detail", "Maps", new { id = resourceID });
			else if (springBattleID.HasValue) return RedirectToAction("Detail", "Battles", new { id = springBattleID });
			else return RedirectToAction("Thread", new { id = thread.ForumThreadID });
		}

		public ActionResult Thread(int id, bool? lastPost, bool? lastSeen, int? page = 0)
		{
			var db = new ZkDataContext();
			var t = db.ForumThreads.FirstOrDefault(x => x.ForumThreadID == id);
			var cat = t.ForumCategory;
			if (cat != null)
			{
				if (cat.IsMissions) return RedirectToAction("Detail", "Missions", new { id = t.Missions.MissionID });
				if (cat.IsMaps) return RedirectToAction("Detail", "Maps", new { id = t.Resources.ResourceID });
				if (cat.IsSpringBattles) return RedirectToAction("Detail", "Battles", new { id = t.SpringBattles.SpringBattleID });
			}

			var res = new ThreadResult();
			res.GoToPost = t.UpdateLastRead(Global.AccountID, false);

			db.SubmitChanges();

			res.Path = GetCategoryPath(t.ForumCategoryID, db);
			res.CurrentThread = t;
			res.PageCount = (t.PostCount/PageSize) + 1;
			res.Posts = t.ForumPosts.AsQueryable().Skip((page ?? 0)*PageSize).Take(PageSize).ToList();

			return View(res);
		}

		static IEnumerable<ForumCategory> GetCategoryPath(int? categoryID, ZkDataContext db)
		{
			var path = new List<ForumCategory>();
			var id = categoryID;
			while (id != null)
			{
				var cat = db.ForumCategories.SingleOrDefault(x => x.ForumCategoryID == id);
				path.Add(cat);
				id = cat.ParentForumCategoryID;
			}
			path.Reverse();
			return path;
		}

		public class IndexResult
		{
			public IEnumerable<ForumCategory> Categories;
			public ForumCategory CurrentCategory;
			public IEnumerable<ForumCategory> Path;
			public IEnumerable<ForumThread> Threads;
		}

		public class NewPostResult
		{
			public ForumCategory CurrentCategory;
			public ForumThread CurrentThread;
			public IEnumerable<ForumPost> LastPosts;
			public IEnumerable<ForumCategory> Path;
		}

		public class ThreadResult
		{
			public ForumThread CurrentThread;
			public int GoToPost;
			public int PageCount;
			public IEnumerable<ForumCategory> Path;
			public List<ForumPost> Posts;
		}
	}
}