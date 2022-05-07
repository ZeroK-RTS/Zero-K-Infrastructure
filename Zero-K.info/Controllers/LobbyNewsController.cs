using System;
using System.Collections.Generic;
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
    public class LobbyNewsController : Controller
    {
        public ActionResult Index()
        {
            var db = new ZkDataContext();
            var items = db.LobbyNews.OrderBy(x=>x.PinnedOrder ?? int.MaxValue).ThenByDescending(x => x.Created).Take(10);
            return View("LobbyNewsIndex", items);
        }

        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult Edit(int? id)
        {
            var db=  new ZkDataContext();
            return View("LobbyNewsEdit", db.LobbyNews.Find(id));
        }

        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult Delete(int id)
        {
            var db = new ZkDataContext();
            var n = db.LobbyNews.Find(id);
            db.LobbyNews.Remove(n);
            db.SaveChanges();
            Global.Server.NewsListManager.OnNewsChanged();
            return RedirectToAction("Index");
        }


        /// <summary>
        /// Make a new <see cref="News"/> item or edit an existing one
        /// </summary>
        /// <param name="nn">The existing <see cref="News"/> item, if editing</param>
        /// <remarks>Also makes or edits a <see cref="ForumThread"/> and its starting <see cref="ForumPost"/></remarks>
        [ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        [ValidateInput(false)]
        public ActionResult PostNews(LobbyNews nn, HttpPostedFileBase image)
        {
            if (string.IsNullOrEmpty(nn.Title) || string.IsNullOrEmpty(nn.Text)) return Content("Empty text!");

            var db = new ZkDataContext();
            using (var scope = new TransactionScope())
            {
                LobbyNews news;
                if (nn.LobbyNewsID == 0)
                {
                    news = new LobbyNews();
                    db.LobbyNews.Add(news);
                }
                else
                {
                    news = db.LobbyNews.Single(x => x.LobbyNewsID == nn.LobbyNewsID);
                }
                news.AuthorAccountID = Global.AccountID;
                news.Title = nn.Title;
                news.Text = nn.Text;
                news.Url = nn.Url;
                news.EventTime = nn.EventTime;
                news.PinnedOrder = nn.PinnedOrder;
                

                Image im = null;
                if (image != null && image.ContentLength > 0)
                {
                    im = Image.FromStream(image.InputStream);
                    news.ImageExtension = Path.GetExtension(image.FileName);
                }

                db.SaveChanges();

                if (im != null)
                {
                    Image thumb = im.GetResized(256, (int)Math.Round(256.0 / im.Width * im.Height), InterpolationMode.HighQualityBicubic);
                    var targetPath = Server.MapPath(news.ImageRelativeUrl);
                    var folder = Path.GetDirectoryName(targetPath);
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                    thumb.Save(targetPath);
                }
                scope.Complete();
            }
            Global.Server.NewsListManager.OnNewsChanged();

            return RedirectToAction("Index", "LobbyNews");
        }


    }
}