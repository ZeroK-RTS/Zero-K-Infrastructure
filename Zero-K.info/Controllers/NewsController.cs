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
		public ActionResult PostNews(int? newsID, string heading, string text, DateTime created, int? headlineDays, HttpPostedFileBase image)
		{
			if (string.IsNullOrEmpty(heading) || string.IsNullOrEmpty(text)) return Content("Empty text!");

			var db = new ZkDataContext();
			using (var scope = new TransactionScope())
			{
			    News news;
                if (newsID == null)
                {
                    news = new News();
                    news.Created = created;
                }
                else news = db.News.Single(x => x.NewsID == newsID);
		        news.AuthorAccountID = Global.AccountID;
                news.Title=  heading;
                news.Text = text;

				Image im = null;
				if (image != null && image.ContentLength > 0)
				{
					im = Image.FromStream(image.InputStream);
					news.ImageExtension = Path.GetExtension(image.FileName);
					news.ImageContentType = image.ContentType;
					news.ImageLength = image.ContentLength;
				}

				if (headlineDays.HasValue && headlineDays.Value > 0) news.HeadlineUntil = news.Created.AddDays(headlineDays.Value);

                if (newsID == null)
                {
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
                }
                
			    db.SubmitChanges();
				if (im != null)
				{
					im.Save(Server.MapPath(news.ImageRelativeUrl));
				}
				scope.Complete();
			}
			if (newsID == null) MakeSpringNewsPosts();
			return Content("Posted!");
		}
	}
}