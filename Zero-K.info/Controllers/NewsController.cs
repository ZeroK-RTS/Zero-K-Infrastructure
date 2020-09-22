using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using ZkData;
using PlasmaShared;

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

        /// <summary>
        /// Make a new <see cref="News"/> item or edit an existing one
        /// </summary>
        /// <param name="nn">The existing <see cref="News"/> item, if editing</param>
        /// <remarks>Also makes or edits a <see cref="ForumThread"/> and its starting <see cref="ForumPost"/></remarks>
        [ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        [ValidateInput(false)]
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
                string postText = news.Text;

                if (nn.NewsID == 0)
                {
                    var thread = new ForumThread()
                                 {
                                     Created = news.Created,
                                     CreatedAccountID = news.AuthorAccountID,
                                     Title = news.Title,
                                     ForumCategoryID = db.ForumCategories.Single(x => x.ForumMode == ForumMode.News).ForumCategoryID
                                 };

                    
                    thread.ForumPosts.Add(new ForumPost() { Created = news.Created, Text = postText, AuthorAccountID = news.AuthorAccountID });
                    db.ForumThreads.InsertOnSubmit(thread);
                    db.SaveChanges();
                    news.ForumThreadID = thread.ForumThreadID;
                    db.News.InsertOnSubmit(news);
                }
                else
                {
                    nn.ForumThread.Title = nn.Title;
                }
			    db.SaveChanges();

			    // add image to the start of the forum post we made
                // do it down here so it gets the correct news ID
                if (!String.IsNullOrWhiteSpace(news.ImageRelativeUrl) && news.ForumThread != null)
                {
                    postText = "[img]" + GlobalConst.BaseSiteUrl + news.ImageRelativeUrl + "[/img]" + Environment.NewLine + postText;
                    news.ForumThread.ForumPosts.ElementAt(0).Text = postText;
                    db.SaveChanges();
                }

				if (im != null)
				{
					im.Save(Server.MapPath(news.ImageRelativeUrl));
                    Image thumb = im.GetResized(120, (int)Math.Round(120.0 / im.Width * im.Height), InterpolationMode.HighQualityBicubic);
                    thumb.Save(Server.MapPath(news.ThumbRelativeUrl));
				}
				scope.Complete();
			}
		    return RedirectToAction("Index", "Home");
		}
	}
}
