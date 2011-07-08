using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using PlasmaShared;
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

		public void MakeSpringNewsPosts()
		{
			var db =new ZkDataContext();
			foreach (News n in db.News.Where(x=>x.SpringForumPostID == null && x.Created <= DateTime.UtcNow).OrderBy(x=>x.Created)) {
				string bbuid = SpringForumController.GenBBUid();
				string text = string.Format("[url={0}:{1}][size=150:{1}]  [b:{1}]{2}[/b:{1}][/size:{1}][/url:{1}]\n {3}",
																		Url.Action("Thread", "Forum", new { id= n.ForumThreadID},"http"),
																		bbuid,
																		n.Title,
																		n.Text);
				
				n.SpringForumPostID = SpringForumController.PostOrEdit(text, bbuid, n.SpringForumPostID, SpringForumController.TopicIdNews, n.Title, n.Created);
				db.SubmitChanges();				
			}
		}


		[Auth]
		public ActionResult PostNews(int? newsID, string title, string text, DateTime created, int? headlineDays, HttpPostedFileBase image)
		{
			if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(text)) return Content("Empty text!");

			var db = new ZkDataContext();
			using (var scope = new TransactionScope())
			{
				var news = new News() { AuthorAccountID = Global.AccountID, Created = created, Title = title, Text = text, };

				Image im = null;
				if (image != null && image.ContentLength > 0)
				{
					im = Image.FromStream(image.InputStream);
					news.ImageExtension = Path.GetExtension(image.FileName);
					news.ImageContentType = image.ContentType;
					news.ImageLength = image.ContentLength;
				}

				if (headlineDays.HasValue && headlineDays.Value > 0) news.HeadlineUntil = news.Created.AddDays(headlineDays.Value);
				var thread = new ForumThread()
				             {
				             	Created = news.Created,
				             	CreatedAccountID = news.AuthorAccountID,
				             	Title = news.Title,
				             	ForumCategoryID = db.ForumCategories.Single(x => x.IsNews).ForumCategoryID
				             };
				thread.ForumPosts.Add(new ForumPost() { Created = news.Created, Text = news.Text, AuthorAccountID = news.AuthorAccountID });
				db.ForumThreads.InsertOnSubmit(thread);
				db.SubmitChanges();
				news.ForumThreadID = thread.ForumThreadID;
				db.News.InsertOnSubmit(news);
				db.SubmitChanges();
				if (im != null)
				{
					im.Save(Server.MapPath(news.ImageRelativeUrl));
				}
				scope.Complete();
			}
			MakeSpringNewsPosts();
			return Content("Posted!");
		}
	}
}