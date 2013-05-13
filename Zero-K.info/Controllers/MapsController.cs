using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using PlasmaShared;
using PlasmaShared.UnitSyncLib;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class MapsController: Controller
    {
        //
        // GET: /Maps/

        public ActionResult Detail(int id) {
            var db = new ZkDataContext();
            var res = db.Resources.Single(x => x.ResourceID == id);
            var data = GetMapDetailData(res, db);

            return View(data);
        }

        public ActionResult DetailName(string name) {
            var db = new ZkDataContext();
            var res = db.Resources.Single(x => x.InternalName == name);

            return View("Detail", GetMapDetailData(res, db));
        }


        public ActionResult Index(string search,
                                  bool? featured,
                                  int? offset,
                                  bool? ffa,
                                  bool? assymetrical,
                                  int? sea,
                                  int? hills,
                                  int? size,
                                  bool? elongated,
                                  bool? needsTagging,
                                  bool? is1v1,
                                  bool? chicken,
                                  int? isDownloadable = 1,
                                  int? special = 0) {
            IQueryable<Resource> ret;
            var db = FilterMaps(search,
                                featured,
                                offset,
                                ffa,
                                assymetrical,
                                sea,
                                hills,
                                size,
                                elongated,
                                needsTagging,
                                is1v1,
                                chicken,
                                isDownloadable,
                                special,
                                out ret);

            if (!offset.HasValue) {
                return
                    View(new MapIndexData
                    {
                        Title = "Latest maps",
                        Latest = ret,
                        LastComments =
                            db.Resources.Where(x => x.TypeID == ResourceType.Map && x.ForumThreadID != null)
                              .OrderByDescending(x => x.ForumThread.LastPost),
                        TopRated =
                            db.Resources.Where(x => x.TypeID == ResourceType.Map && x.MapRatingCount > 0)
                              .OrderByDescending(x => x.MapRatingSum/x.MapRatingCount),
                        MostDownloads = db.Resources.Where(x => x.TypeID == ResourceType.Map).OrderByDescending(x => x.DownloadCount)
                    });
            }

            else {
                if (ret.Any()) return View("MapTileList", ret);
                else return Content("");
            }
        }

        public ContentResult JsonSearch(string callback, string search,
                                       bool? featured,
                                       int? offset,
                                       bool? ffa,
                                       bool? assymetrical,
                                       int? sea,
                                       int? hills,
                                       int? size,
                                       bool? elongated,
                                       bool? needsTagging,
                                       bool? is1v1,
                                       bool? chicken,
                                       int? isDownloadable = 1,
                                       int? special = 0) {
            IQueryable<Resource> ret;
            var db = FilterMaps(search,
                                featured,
                                offset,
                                ffa,
                                assymetrical,
                                sea,
                                hills,
                                size,
                                elongated,
                                needsTagging,
                                is1v1,
                                chicken,
                                isDownloadable,
                                special,
                                out ret);
            var retval =
                ret.Select(
                    x =>
                    new
                    {
                        x.AuthorName,
                        x.InternalName,
                        x.DownloadCount,
                        x.FeaturedOrder,
                        x.ForumThreadID,
                        x.HeightmapName,
                        x.LastChange,
                        x.LastLinkCheck,
                        x.MapDiagonal,
                        x.MapFFAMaxTeams,
                        x.MapHeight,
                        x.MapHills,
                        x.MapIs1v1,
                        x.MapIsAssymetrical,
                        x.MapIsChickens,
                        x.MapIsFfa,
                        x.MapIsSpecial,
                        x.MapPlanetWarsIcon,
                        x.MapRating,
                        x.MapRatingCount,
                        x.MapRatingSum,
                        x.MapWaterLevel,
                        x.MapSpringieCommands,
                        x.MapSizeRatio,
                        x.MapSizeSquared,
                        x.MapWidth,
                        x.MetadataName,
                        x.MetalmapName,
                        x.MinimapName,
                        x.MissionID,
                        x.PlanetWarsIconSize,
                        x.RatingPollID,
                        x.ThumbnailName
                    }).ToList();
            return Content(String.Format("{0}({1});",callback,new JavaScriptSerializer().Serialize(retval)),"application/javascript");
        }

        public ActionResult PlanetImageSelect(int resourceID) {
            var res = new PlanetImageSelectData();
            var db = new ZkDataContext();
            var map = db.Resources.Single(x => x.ResourceID == resourceID);
            res.ResourceID = resourceID;
            res.IconSize = map.PlanetWarsIconSize;
            res.Icons = Directory.GetFiles(Server.MapPath("/img/planets")).Select(Path.GetFileName).ToList();
            return View("PlanetImageSelect", res);
        }

        [Auth]
        public ActionResult Rate(int id, int rating) {
            var db = new ZkDataContext();
            var rat = db.MapRatings.SingleOrDefault(x => x.ResourceID == id && x.AccountID == Global.Account.AccountID);
            if (rat == null) {
                rat = new MapRating();
                db.MapRatings.InsertOnSubmit(rat);
            }
            rat.ResourceID = id;
            rat.AccountID = Global.Account.AccountID;
            rat.Rating = rating;
            db.SubmitChanges();
            rat.Resource.MapRatingSum = rat.Resource.MapRatings.Sum(x => x.Rating);
            rat.Resource.MapRatingCount = rat.Resource.MapRatings.Count();
            db.SubmitChanges();

            return Content("");
        }

        public ActionResult RemovePlanetIcon(int resourceID) {
            var db = new ZkDataContext();
            var res = db.Resources.Single(x => x.ResourceID == resourceID);
            res.MapPlanetWarsIcon = null;
            db.SubmitChanges();
            return RedirectToAction("Detail", new { id = res.ResourceID });
        }

        public ActionResult SubmitPlanetIcon(int resourceID, string icon) {
            var db = new ZkDataContext();
            var res = db.Resources.Single(x => x.ResourceID == resourceID);
            res.MapPlanetWarsIcon = icon;

            db.SubmitChanges();
            return RedirectToAction("Detail", new { id = res.ResourceID });
        }

        [Auth]
        public ActionResult Tag(int id,
                                bool? special,
                                int? sea,
                                int? hills,
                                bool? ffa,
                                bool? assymetrical,
                                string author,
                                float? featuredOrder,
                                bool? is1v1,
                                bool? chickens,
                                int? ffaTeams,
                                string springieCommands) {
            var db = new ZkDataContext();
            var r = db.Resources.Single(x => x.ResourceID == id);
            r.TaggedByAccountID = Global.AccountID;
            r.MapIsSpecial = special;
            r.MapWaterLevel = sea;
            r.MapHills = hills;
            r.MapIsFfa = ffa;
            r.MapIsAssymetrical = assymetrical;
            r.AuthorName = author;
            r.MapIs1v1 = is1v1;
            r.MapIsChickens = chickens;
            if (Global.Account.IsZeroKAdmin) {
                r.FeaturedOrder = featuredOrder;
                r.MapFFAMaxTeams = ffaTeams;
                r.MapSpringieCommands = springieCommands;
            }
            db.SubmitChanges();
            var order = 1;
            if (featuredOrder.HasValue) foreach (var map in db.Resources.Where(x => x.FeaturedOrder != null).OrderBy(x => x.FeaturedOrder)) map.FeaturedOrder = order++;
            db.SubmitChanges();
            return RedirectToAction("Detail", new { id = id });
        }

        static ZkDataContext FilterMaps(string search,
                                        bool? featured,
                                        int? offset,
                                        bool? ffa,
                                        bool? assymetrical,
                                        int? sea,
                                        int? hills,
                                        int? size,
                                        bool? elongated,
                                        bool? needsTagging,
                                        bool? is1v1,
                                        bool? chicken,
                                        int? isDownloadable,
                                        int? special,
                                        out IQueryable<Resource> ret) {
            var db = new ZkDataContext();
            if (featured == null) featured = true;

            ret = db.Resources.Where(x => x.TypeID == ResourceType.Map);
            if (!string.IsNullOrEmpty(search)) {
                foreach (var word in search.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)) {
                    var w = word;
                    ret = ret.Where(x => x.InternalName.Contains(w) || x.AuthorName.Contains(w));
                }
            }

            if (needsTagging == true) {
                ret =
                    ret.Where(
                        x =>
                        x.MapIsFfa == null || x.MapIsAssymetrical == null || x.MapIsSpecial == null || x.AuthorName == null || x.MapHills == null ||
                        x.MapWaterLevel == null);
            }

            if (featured == true) ret = ret.Where(x => x.FeaturedOrder > 0);
            if (isDownloadable == 1) ret = ret.Where(x => x.ResourceContentFiles.Any(y => y.LinkCount > 0));
            else if (isDownloadable == 0) ret = ret.Where(x => x.ResourceContentFiles.All(y => y.LinkCount <= 0));
            if (special != -1) ret = ret.Where(x => x.MapIsSpecial == (special == 1));
            if (ffa.HasValue) ret = ret.Where(x => x.MapIsFfa == ffa);
            if (sea.HasValue) ret = ret.Where(x => x.MapWaterLevel == sea);
            if (hills.HasValue) ret = ret.Where(x => x.MapHills == hills);
            if (assymetrical.HasValue) ret = ret.Where(x => x.MapIsAssymetrical == assymetrical);
            if (elongated == true) ret = ret.Where(x => x.MapSizeRatio <= 0.5 || x.MapSizeRatio >= 2);
            else if (elongated == false) ret = ret.Where(x => x.MapSizeRatio > 0.5 && x.MapSizeRatio < 2);
            // Diagonal of a map used to determine size; 16 and below are considered small, bigger than 24 is large
            if (size == 1) ret = ret.Where(x => Math.Sqrt((x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth) ?? 0) <= 16);
            else if (size == 2) {
                ret =
                    ret.Where(
                        x =>
                        Math.Sqrt((x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth) ?? 0) > 16 &&
                        Math.Sqrt((x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth) ?? 0) <= 24);
            }
            else if (size == 3) ret = ret.Where(x => Math.Sqrt((x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth) ?? 0) > 24);
            if (is1v1.HasValue) ret = ret.Where(x => x.MapIs1v1 == is1v1);
            if (chicken.HasValue) ret = ret.Where(x => x.MapIsChickens == chicken);

            if (featured == true) ret = ret.OrderByDescending(x => -x.FeaturedOrder).ThenByDescending(x => x.ResourceID);
            else ret = ret.OrderByDescending(x => x.ResourceID);
            if (offset != null) ret = ret.Skip(offset.Value);
            ret = ret.Take(Global.AjaxScrollCount);
            return db;
        }

        MapDetailData GetMapDetailData(Resource res, ZkDataContext db) {
            var data = new MapDetailData
            {
                Resource = res,
                MyRating = res.MapRatings.SingleOrDefault(x => x.AccountID == Global.AccountID) ?? new MapRating()
            };

            // load map info from disk - or used cached copy if its in memory
            var cachedEntry = HttpContext.Application["mapinfo_" + res.ResourceID] as Map;
            if (cachedEntry != null) data.MapInfo = cachedEntry;
            else {
                var path = Server.MapPath("~/Resources/") + res.MetadataName;
                if (System.IO.File.Exists(path)) {
                    try {
                        data.MapInfo =
                            (Map)new XmlSerializer(typeof(Map)).Deserialize(new MemoryStream(System.IO.File.ReadAllBytes(path).Decompress()));
                        HttpContext.Application["mapinfo_" + res.ResourceID] = data.MapInfo;
                    } catch (Exception ex) {
                        Trace.TraceWarning("Failed to get map metedata {0}:{1}", res.MetadataName, ex);
                        data.MapInfo = new Map();
                    }
                }
            }

            if (res.ForumThread != null) {
                res.ForumThread.UpdateLastRead(Global.AccountID, false);
                db.SubmitChanges();
            }

            return data;
        }


        public class MapDetailData
        {
            public Map MapInfo;
            public MapRating MyRating;
            public Resource Resource;
        }

        public class MapIndexData
        {
            public IQueryable<Resource> LastComments;
            public IQueryable<Resource> Latest;
            public IQueryable<Resource> MostDownloads;
            public string Title;
            public IQueryable<Resource> TopRated;
        }

        public class PlanetImageSelectData
        {
            public int IconSize;
            public List<string> Icons;
            public int ResourceID;
        }
    }
}