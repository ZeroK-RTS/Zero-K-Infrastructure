using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using AutoRegistrator;
using PlasmaShared;
using ZkData.UnitSyncLib;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class RegistrationResult
    {
        public string FileName;
        public string InternalName;
        public string Status;
        public string Url;
        public string Author;
    }
    
    public class MapsController: Controller
    {
        //
        // GET: /Maps/

        public ActionResult Detail(int? id) {
            if (id == null)
              return RedirectToAction("Index");
            var db = new ZkDataContext();
            var res = db.Resources.SingleOrDefault(x => x.ResourceID == id);
            if (res == null)
              return Content("No such map found");
            var data = GetMapDetailData(res, db);

            return View(data);
        }

        /// <summary>
        /// Get map detail page given map name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ActionResult DetailName(string name) {
            if (string.IsNullOrEmpty(name))
              return RedirectToAction("Index");
            var db = new ZkDataContext();
            var res = db.Resources.SingleOrDefault(x => x.InternalName == name);
            if (res == null)
              return Content("No such map found");
            return View("Detail", GetMapDetailData(res, db));
        }

        /// <summary>
        /// Map list; params are for filter
        /// </summary>
        public ActionResult Index(string search,
                                  int? offset,
                                  bool? assymetrical,
                                  int? sea,
                                  int? hills,
                                  int? size,
                                  bool? elongated,
                                  bool? needsTagging,
                                  bool? isTeams,
                                  bool? is1v1,
                                  bool? ffa,
                                  bool? chicken,
                                  int? isDownloadable = 1,
                                  int? special = 0,
                                  MapSupportLevel? mapSupportLevel = null
                                  ) {
            IQueryable<Resource> ret;
            var db = FilterMaps(search,
                                offset,
                                assymetrical,
                                sea,
                                hills,
                                size,
                                elongated,
                                needsTagging,
                                isTeams,
                                is1v1,
                                ffa,
                                chicken,
                                isDownloadable,
                                special,
                                mapSupportLevel,
                                out ret);

            if (!offset.HasValue) {
                // Allow to open maps page with the matchmaking option already set so it can be used in links
                var onlyShowMatchmakerMaps = mapSupportLevel == MapSupportLevel.MatchMaker;
                return
                    View(new MapIndexData
                    {
                        Title = onlyShowMatchmakerMaps ? "Matchmaking maps" : "Latest maps",
                        OnlyShowMatchmakerMaps = onlyShowMatchmakerMaps,
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

        public class EnableCORSAttribute : ActionFilterAttribute
        {
            public override void OnActionExecuting(ActionExecutingContext filterContext)
            {
                filterContext.RequestContext.HttpContext.Response.AddHeader("Access-Control-Allow-Origin", "*");
                base.OnActionExecuting(filterContext);
            }
        }

        [EnableCORS]
        public JsonResult JsonSearch(string search,
                                       int? offset,
                                       bool? assymetrical,
                                       int? sea,
                                       int? hills,
                                       int? size,
                                       bool? elongated,
                                       bool? needsTagging,
                                       bool? isTeams,
                                       bool? is1v1,
                                       bool? ffa,
                                       bool? chicken,
                                       int? isDownloadable = 1,
                                       int? special = 0,
                                       MapSupportLevel? mapSupportLevel = null) {
            IQueryable<Resource> ret;
            var db = FilterMaps(search,
                                offset,
                                assymetrical,
                                sea,
                                hills,
                                size,
                                elongated,
                                needsTagging,
                                isTeams,
                                is1v1,
                                ffa,
                                chicken,
                                isDownloadable,
                                special,
                                mapSupportLevel,
                                out ret);
            var retval =
                ret.ToList().Select(
                    x =>
                    new
                    {
                        x.AuthorName,
                        x.InternalName,
                        x.DownloadCount,
                        x.MapSupportLevel,
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
                        x.MapIsTeams,
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
                    });
            return Json(retval, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Brings up the planet image selector page
        /// </summary>
        /// <param name="resourceID">The ID of the map to assign a planet image to</param>
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
            db.SaveChanges();

            db = new ZkDataContext(); // select again so that linked resources work
            rat = db.MapRatings.First(x => x.ResourceID == rat.ResourceID && x.AccountID == rat.AccountID);
            rat.Resource.MapRatingSum = rat.Resource.MapRatings.Sum(x => x.Rating);
            rat.Resource.MapRatingCount = rat.Resource.MapRatings.Count();
            db.SaveChanges();

            return Content("");
        }

        public ActionResult RemovePlanetIcon(int resourceID) {
            var db = new ZkDataContext();
            var res = db.Resources.Single(x => x.ResourceID == resourceID);
            res.MapPlanetWarsIcon = null;
            db.SaveChanges();
            return RedirectToAction("Detail", new { id = res.ResourceID });
        }

        public ActionResult SubmitPlanetIcon(int resourceID, string icon) {
            var db = new ZkDataContext();
            var res = db.Resources.Single(x => x.ResourceID == resourceID);
            res.MapPlanetWarsIcon = icon;

            db.SaveChanges();
            return RedirectToAction("Detail", new { id = res.ResourceID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        public async Task<ActionResult> Tag(int id,
                                int? sea,
                                int? hills,
                                bool? assymetrical,
                                string author,
                                bool? isTeams,
                                bool? is1v1,
                                bool? ffa,
                                bool? chickens,
                                int? ffaTeams,
                                bool? special,
                                string springieCommands,
                                MapSupportLevel mapSupportLevel) {
            var db = new ZkDataContext();
            var r = db.Resources.Single(x => x.ResourceID == id);

            if (r.MapSupportLevel != mapSupportLevel)
                await Global.Server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format("{0} has changed level of map {1} from {2} to {3}", Global.Account.Name, r.InternalName, r.MapSupportLevel, mapSupportLevel));

            r.TaggedByAccountID = Global.AccountID;
            r.MapIsSpecial = special;
            r.MapWaterLevel = sea;
            r.MapHills = hills;
            r.MapIsAssymetrical = assymetrical;
            r.AuthorName = author;
            r.MapIsTeams = isTeams;
            r.MapIs1v1 = is1v1;
            r.MapIsFfa = ffa;
            r.MapIsChickens = chickens;
            r.MapSupportLevel = mapSupportLevel;
            r.MapFFAMaxTeams = ffaTeams;
            r.MapSpringieCommands = springieCommands;
            db.SaveChanges();
            await Global.Server.OnServerMapsChanged();
            return RedirectToAction("Detail", new { id = id });
        }

        static ZkDataContext FilterMaps(string search,
                                        int? offset,
                                        bool? assymetrical,
                                        int? sea,
                                        int? hills,
                                        int? size,
                                        bool? elongated,
                                        bool? needsTagging,
                                        bool? isTeams,
                                        bool? is1v1,
                                        bool? ffa,
                                        bool? chicken,
                                        int? isDownloadable,
                                        int? special,
                                        MapSupportLevel? mapSupportLevel,
                                        out IQueryable<Resource> ret) {
            var db = new ZkDataContext();

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

            if (mapSupportLevel != null)
            {   // unsupported == only unsupported; anything else also selects maps with higher support level
                if (mapSupportLevel == 0) ret = ret.Where(x => x.MapSupportLevel == mapSupportLevel);
                ret = ret.Where(x => x.MapSupportLevel >= mapSupportLevel);
            }
            if (isDownloadable == 1) ret = ret.Where(x => x.ResourceContentFiles.Any(y => y.LinkCount > 0));
            else if (isDownloadable == 0) ret = ret.Where(x => x.ResourceContentFiles.All(y => y.LinkCount <= 0));
            if (special != -1) ret = ret.Where(x => x.MapIsSpecial == (special == 1));
            
            if (sea.HasValue) ret = ret.Where(x => x.MapWaterLevel == sea);
            if (hills.HasValue) ret = ret.Where(x => x.MapHills == hills);
            if (assymetrical.HasValue) ret = ret.Where(x => x.MapIsAssymetrical == assymetrical);
            if (elongated == true) ret = ret.Where(x => x.MapSizeRatio <= 0.5 || x.MapSizeRatio >= 2);
            else if (elongated == false) ret = ret.Where(x => x.MapSizeRatio > 0.5 && x.MapSizeRatio < 2);
            // Diagonal of a map used to determine size; 16 and below are considered small, bigger than 24 is large
            if (size == 1) ret = ret.Where(x => (x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth) <= 16*16);
            else if (size == 2) {
                ret =
                    ret.Where(
                        x =>
                        (x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth) > 16*16 &&
                        (x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth) <= 24*24);
            }
            else if (size == 3) ret = ret.Where(x =>(x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth) > 24*24);
            if (isTeams.HasValue) ret = ret.Where(x => x.MapIsTeams == isTeams);
            if (is1v1.HasValue) ret = ret.Where(x => x.MapIs1v1 == is1v1);
            if (chicken.HasValue) ret = ret.Where(x => x.MapIsChickens == chicken);
            if (ffa.HasValue) ret = ret.Where(x => x.MapIsFfa == ffa);

            //if (featured == true) ret = ret.OrderByDescending(x => -x.FeaturedOrder).ThenByDescending(x => x.ResourceID);
            //else ret = ret.OrderByDescending(x => x.ResourceID);
            ret = ret.OrderByDescending(x => x.MapSupportLevel).ThenByDescending(x=>x.ResourceID);
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
                        HttpContext.Application["mapinfo_" + res.ResourceID] = data.MapInfo;
                    }
                }
            }

            if (res.ForumThread != null) {
                res.ForumThread.UpdateLastRead(Global.AccountID, false);
                db.SaveChanges();
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
            public Boolean OnlyShowMatchmakerMaps;
            public IQueryable<Resource> TopRated;
        }

        public class PlanetImageSelectData
        {
            public int IconSize;
            public List<string> Icons;
            public int ResourceID;
        }

        [Auth]
        public ActionResult UploadResource(HttpPostedFileBase file, bool specialMap)
        {
            var tmp = Path.Combine(Global.AutoRegistrator.Paths.WritableDirectory, "maps", file.FileName);
            try
            {
                file.SaveAs(tmp);
                var results = Global.AutoRegistrator.UnitSyncer.Scan()?.Where(x=>x.ResourceInfo?.ArchiveName == file.FileName)?.ToList();
                var model = new List<RegistrationResult>();
                foreach (var res in results)
                {
                    if (res.Status != UnitSyncer.ResourceFileStatus.RegistrationError)
                    {
                        // copy to content subfolder
                        var subfolder = (res.ResourceInfo is Map) ? "maps" : "games";
                        var contentFolder = Path.Combine(Server.MapPath("~/content"), subfolder);
                        if (!Directory.Exists(contentFolder)) Directory.CreateDirectory(contentFolder);

                        var destFile = Path.Combine(contentFolder, res.ResourceInfo.ArchiveName);
                        if (!System.IO.File.Exists(destFile)) System.IO.File.Copy(tmp, destFile);

                        
                        // register as mirror
                        using (var db = new ZkDataContext())
                        {
                            var resource = db.Resources.FirstOrDefault(x => x.InternalName == res.ResourceInfo.Name);
                            var contentFile = resource.ResourceContentFiles.FirstOrDefault(x => x.FileName == file.FileName);
                            contentFile.Links = $"{GlobalConst.BaseSiteUrl}/content/{subfolder}/{file.FileName}";
                            contentFile.LinkCount = 1;

                            // tag as special if required
                            if (res.Status == UnitSyncer.ResourceFileStatus.Registered && specialMap)
                            {
                                resource.MapIsSpecial = true;
                            }

                            db.SaveChanges();
                        }
                        
                    }
                    
                    // note this is needed because of some obscure binding issue in asp.net
                    model.Add(new RegistrationResult()
                    {
                        Status = res.Status.ToString(),
                        FileName = file.FileName,
                        InternalName = res.ResourceInfo?.Name,
                        Author = res.ResourceInfo?.Author,
                        Url = Url.Action("Detail", new {id = new ZkDataContext().Resources.FirstOrDefault(x => x.InternalName == res.ResourceInfo.Name)?.ResourceID})
                    });
                    
                }
                return View("UploadResourceResult", model);
            }
            finally
            {
                Task.Run(async () =>
                {
                    await Task.Delay(10000);
                    try
                    {
                        System.IO.File.Delete(tmp);
                    }
                    catch { }
                });
            }
        }
    }
}
