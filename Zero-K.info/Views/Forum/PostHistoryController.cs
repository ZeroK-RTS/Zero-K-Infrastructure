using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ZeroKWeb.ForumParser;
using ZkData;

namespace ZeroKWeb.Views.Forum
{
    public class PostHistoryController : Controller
    {
        public class PostHistoryModel
        {
            public int ID { get; set; }
            public string User { get; set; }
        }

        public ActionResult Index(int id, List<int> grSel) {
            var db = new ZkDataContext();
            var post = db.ForumPosts.Find(id);

            return View("PostHistoryIndex", post);
        }

        public ActionResult Diff() {
            return null;

            


        }

        public ActionResult RevertBefore(int id) {
            var db = new ZkDataContext();
            var edit = db.ForumPostEdits.Find(id);

            throw new NotImplementedException();
        }
    }
}