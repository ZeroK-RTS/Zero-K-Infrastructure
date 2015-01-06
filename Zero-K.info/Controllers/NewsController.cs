using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Transactions;
using System.Web;
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

        public ActionResult Detail(int id) {
            var db = new ZkDataContext();
            var news = db.News.Single(x => x.NewsID == id);
            return View("NewsDetail", news);
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

                    string postText = news.Text;
                    if (!String.IsNullOrWhiteSpace(news.ImageRelativeUrl))
                    {
                        postText = "[img]" + news.ImageRelativeUrl + "[/img]" + Environment.NewLine + postText;
                    }
                    thread.ForumPosts.Add(new ForumPost() { Created = news.Created, Text = postText, AuthorAccountID = news.AuthorAccountID });
                    db.ForumThreads.InsertOnSubmit(thread);
                    db.SubmitChanges();
                    news.ForumThreadID = thread.ForumThreadID;
                    db.News.InsertOnSubmit(news);
                }
                
			    db.SubmitChanges();
				if (im != null)
				{
					im.Save(Server.MapPath(news.ImageRelativeUrl));
                    Image thumb = im.GetResized(120, (int)Math.Round(120.0 / im.Width * im.Height), InterpolationMode.HighQualityBicubic);
                    thumb.Save(Server.MapPath(news.ThumbRelativeUrl));
				}
				scope.Complete();
			}
			return Content("Posted!");
		}
	}
}