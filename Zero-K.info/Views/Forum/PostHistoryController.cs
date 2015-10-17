using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Services.Description;
using ZeroKWeb.Controllers;
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



        public ActionResult RevertTo(int id, bool? isAfter = false) {
            var db = new ZkDataContext();
            var edit = db.ForumPostEdits.Find(id);
            var post = edit.ForumPost;
            var thread = post.ForumThread;
            if (edit.ForumPost.CanEdit(Global.Account))
            {
                var fc = (ForumController)DependencyResolver.Current.GetService(typeof(ForumController));
                return fc.SubmitPost(
                    thread.ForumThreadID,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    isAfter == true?  edit.NewText: edit.OriginalText,
                    thread.Title,
                    thread.WikiKey,
                    post.ForumPostID);
            } else return Content("Denied");
        }

        public ActionResult ViewEntry(int id, bool? isAfter = false) {
            var db = new ZkDataContext();
            var edit = db.ForumPostEdits.Find(id);
            return View("ViewEntry", (object)(isAfter == true? edit.NewText : edit.OriginalText));

        }
    }
}