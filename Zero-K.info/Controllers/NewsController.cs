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

        public ActionResult Detail(int id) {
            var db = new ZkDataContext();
            var news = db.News.Single(x => x.NewsID == id);
            return View("NewsDetail", news);
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


        [Auth(Role = AuthRole.LobbyAdmin | AuthRole.ZkAdmin)]
		public ActionResult PostNews(News nn, HttpPostedFileBase image)
		{
			if (string.IsNullOrEmpty(nn.Title) || string.IsNullOrEmpty(nn.Text)) return Content("Empty text!");

			var db = new ZkDataContext();
			using (var scope = new TransactionScope())
			{
			    News news;
                if (nn.NewsID == 0)
                {
                    news = new News();
                    news.Created = nn.Created;
                }
                else news = db.News.Single(x => x.NewsID == nn.NewsID);
		        news.AuthorAccountID = Global.AccountID;
                news.Title=  nn.Title;
                news.Text = nn.Text;

				Image im = null;
				if (image != null && image.ContentLength > 0)
				{
					im = Image.FromStream(image.InputStream);
					news.ImageExtension = Path.GetExtension(image.FileName);
					news.ImageContentType = image.ContentType;
					news.ImageLength = image.ContentLength;
				}

                news.HeadlineUntil = nn.HeadlineUntil;

                if (nn.NewsID == 0)
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
                    Image thumb = im.GetResized(120, 120 / im.Width * im.Height, InterpolationMode.HighQualityBicubic);
                    thumb.Save(Server.MapPath(news.ThumbRelativeUrl));
				}
				scope.Complete();
			}
			if (nn.NewsID == 0) MakeSpringNewsPosts();
			return Content("Posted!");
		}
	}
}