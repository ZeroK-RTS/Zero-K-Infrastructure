using System;
using System.Linq;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{
	public class NewsController: Controller
	{
		//
		// GET: /News/
		public ActionResult Index()
		{
			Response.ClearContent();
			Response.ContentType = "application/rss+xml";
			var db = new ZkDataContext();
			return View(db.News.Where(x=>x.Created < DateTime.UtcNow).OrderByDescending(x=>x.Created));
		}


		[Auth]	
		public ActionResult PostNews(int? newsID, string title, string text, DateTime created, int? headlineDays)
		{
			if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(text)) return Content("Empty text!");
			var db = new ZkDataContext();
			var news = new News() {
				AuthorAccountID = Global.AccountID,
				Created = created,
				Title = title,
				Text = text,
			};
			if (headlineDays.HasValue && headlineDays.Value > 0) news.HeadlineUntil = news.Created.AddDays(headlineDays.Value);
			var thread = new ForumThread() {
				Created = news.Created,
				CreatedAccountID = news.AuthorAccountID,
				Title = news.Title,
				ForumCategoryID = db.ForumCategories.Single(x=>x.IsNews).ForumCategoryID
			};
			thread.ForumPosts.Add(new ForumPost() {
				Created = news.Created,
				Text = news.Text,
				AuthorAccountID = news.AuthorAccountID
			});
			db.ForumThreads.InsertOnSubmit(thread);
			db.SubmitChanges();
			news.ForumThreadID = thread.ForumThreadID;
			db.News.InsertOnSubmit(news);
			db.SubmitChanges();
			return Content("Posted!");
		}
	}
}