using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class AutocompleteController: Controller
    {
        const int autocompleteCount = 10;


        // general autocomplete for main searchbox
        public ActionResult Index(string term) {
            var db = new ZkDataContext();
            var ret = new List<AutocompleteItem>();

            ret.AddRange(CompleteUsers(term, db));
            ret.AddRange(CompleteThreads(term, db));
            ret.AddRange(CompleteMissions(term, db));
            ret.AddRange(CompleteMaps(term, db));

            return Json(ret, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Users(string term) {
            return Json(CompleteUsers(term, new ZkDataContext()), JsonRequestBehavior.AllowGet);
        }

        public ActionResult Maps(string term)
        {
            return Json(CompleteMaps(term, new ZkDataContext()), JsonRequestBehavior.AllowGet);
        }

        public ActionResult Missions(string term)
        {
            return Json(CompleteMissions(term, new ZkDataContext()), JsonRequestBehavior.AllowGet);
        }

        public ActionResult Threads(string term)
        {
            return Json(CompleteThreads(term, new ZkDataContext()), JsonRequestBehavior.AllowGet);
        }




        IEnumerable<AutocompleteItem> CompleteMissions(string term, ZkDataContext db) {
            return
                db.Missions.Where(x => !x.IsDeleted && x.Name.Contains(term))
                    .Take(autocompleteCount)
                    .ToList()
                    .Select(
                        x =>
                            new AutocompleteItem
                            {
                                label = "Mission " + x.Name,
                                id = x.MissionID,
                                url = Url.Action("Detail", "Missions", new { id = x.MissionID }),
                                value = x.Name
                            });
        }

        IEnumerable<AutocompleteItem> CompleteThreads(string term, ZkDataContext db) {
            return
                db.ForumThreads.Where(x => (x.WikiKey != null && x.WikiKey.Contains(term)) || x.Title.Contains(term))
                    .OrderByDescending(x => x.LastPost)
                    .Take(autocompleteCount)
                    .ToList()
                    .Select(
                        x =>
                            new AutocompleteItem
                            {
                                label = HtmlHelperExtensions.Print(null, x).ToString(),
                                url = Url.Action("Thread", "Forum", new { id = x.ForumThreadID }),
                                value = x.WikiKey ?? x.Title,
                                id = x.ForumThreadID
                            });
        }

        IEnumerable<AutocompleteItem> CompleteMaps(string term, ZkDataContext db) {
            return
                db.Resources.Where(x => x.InternalName.Contains(term) && x.TypeID == ResourceType.Map)
                    .OrderBy(x => x.FeaturedOrder)
                    .Take(autocompleteCount)
                    .ToList()
                    .Select(
                        x =>
                            new AutocompleteItem
                            {
                                label = HtmlHelperExtensions.PrintMap(null, x.InternalName).ToString(),
                                url = Url.Action("Detail", "Maps", new { id = x.ResourceID }),
                                value = x.InternalName,
                                id = x.ResourceID
                            });
        }

        IEnumerable<AutocompleteItem> CompleteUsers(string term, ZkDataContext db) {
            return
                db.Accounts.Where(x => x.Name.Contains(term) && !x.IsDeleted)
                    .Take(autocompleteCount)
                    .ToList()
                    .Select(
                        x =>
                            new AutocompleteItem
                            {
                                label = HtmlHelperExtensions.PrintAccount(null, x).ToString(),
                                url = Url.Action("Detail", "Users", new { id = x.AccountID }),
                                value = x.Name,
                                id = x.AccountID
                            });
        }

        public class AutocompleteItem
        {
            public int id;
            public string label;
            public string url;
            public string value;
        }
    }
}