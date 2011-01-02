using System;
using System.Collections.Generic;
using System.Data.Linq.SqlClient;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Xml.Serialization;
using PlasmaShared;
using PlasmaShared.UnitSyncLib;
using ZeroKWeb;
using ZkData;

namespace ZeroKWeb.Controllers
{
  public class MapsController: Controller
  {
    //
    // GET: /Maps/

    public ActionResult Detail(int id)
    {
      var db = new ZkDataContext();
      var res = db.Resources.Single(x => x.ResourceID == id);

      var data = GetMapDetailData(res);

      return View(data);
    }

    public ActionResult DetailName(string name)
    {
      var db = new ZkDataContext();
      var res = db.Resources.Single(x => x.InternalName == name);

      return View("Detail", GetMapDetailData(res));
    }


    public ActionResult Index(string search,
                              int? offset,
                              bool? ffa,
                              bool? assymetrical,
                              int? sea,
                              int? hills,
                              int? size,
                              bool? elongated,
                              bool? needsTagging,
                              int? isDownloadable = 1,
                              int? special = 0)
    {
      var db = new ZkDataContext();

      var ret = db.Resources.Where(x => x.TypeID == ResourceType.Map);
      if (!string.IsNullOrEmpty(search))
      {
        foreach (var word in search.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
          var w = word;
          ret = ret.Where(x => SqlMethods.Like(x.InternalName, '%' + w + '%') || SqlMethods.Like(x.AuthorName, '%' + w + '%'));
        }
      }

      if (needsTagging == true)
      {
        ret =
          ret.Where(
            x =>
            x.MapIsFfa == null || x.MapIsAssymetrical == null || x.MapIsSpecial == null || x.AuthorName == null || x.MapHills == null ||
            x.MapWaterLevel == null);
      }

      if (isDownloadable == 1) ret = ret.Where(x => x.ResourceContentFiles.Any(y => y.LinkCount > 0));
      else if (isDownloadable == 0) ret = ret.Where(x => x.ResourceContentFiles.All(y => y.LinkCount <= 0));
      if (special != -1) ret = ret.Where(x => x.MapIsSpecial == (special == 1));
      if (ffa.HasValue) ret = ret.Where(x => x.MapIsFfa == ffa);
      if (sea.HasValue) ret = ret.Where(x => x.MapWaterLevel == sea);
      if (hills.HasValue) ret = ret.Where(x => x.MapHills == hills);
      if (assymetrical.HasValue) ret = ret.Where(x => x.MapIsAssymetrical == assymetrical);
      if (elongated == true) ret = ret.Where(x => x.MapSizeRatio < 0.5 || x.MapSizeRatio > 2);
      else if (elongated == false) ret = ret.Where(x => x.MapSizeRatio >= 0.5 && x.MapSizeRatio <= 2);
      if (size == 1) ret = ret.Where(x => x.MapHeight <= 12 && x.MapWidth <= 12);
      else if (size == 2) ret = ret.Where(x => (x.MapWidth > 12 || x.MapHeight > 12) && (x.MapWidth <= 20 && x.MapHeight <= 20));
      else if (size == 3) ret = ret.Where(x => x.MapWidth > 20 || x.MapHeight > 20);

      ret = ret.OrderByDescending(x => x.ResourceID);
      if (offset != null) ret = ret.Skip(offset.Value);
      ret = ret.Take(Global.AjaxScrollCount);

      if (!offset.HasValue) return View(new MapIndexData {
        Latest = ret,
        LastComments = db.Resources.Where(x=>x.TypeID == ResourceType.Map && x.ForumThreadID != null).OrderByDescending(x=>x.ForumThread.LastPost),
        TopRated = db.Resources.Where(x => x.TypeID == ResourceType.Map && x.MapRatingCount > 0).OrderByDescending(x => x.MapRatingSum / x.MapRatingCount),
        MostDownloads = db.Resources.Where(x => x.TypeID == ResourceType.Map).OrderByDescending(x => x.DownloadCount)
      });

      else
      {
        if (ret.Any()) return View("MapTileList", ret);
        else return Content("");
      }
    }

    public ActionResult Rate(int id, int rating)
    {
      if (!Global.IsAccountAuthorized) return Content("Not logged in!");
      else
      {
        var db = new ZkDataContext();
        var rat = db.MapRatings.SingleOrDefault(x => x.ResourceID == id && x.AccountID == Global.Account.AccountID);
        if (rat == null)
        {
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
    }

    public ActionResult Tag(int id, bool? special, int? sea, int? hills, bool? ffa, bool? assymetrical, string author)
    {
      if (!Global.IsAccountAuthorized) return Content("Not logged in!");
      else
      {
        var db = new ZkDataContext();
        var r = db.Resources.Single(x => x.ResourceID == id);
        r.TaggedByAccountID = Global.AccountID;
        r.MapIsSpecial = special;
        r.MapWaterLevel = sea;
        r.MapHills = hills;
        r.MapIsFfa = ffa;
        r.MapIsAssymetrical = assymetrical;
        r.AuthorName = author;
        db.SubmitChanges();
        return RedirectToAction("Detail", new { id = id });
      }
    }

    MapDetailData GetMapDetailData(Resource res)
    {
      var data = new MapDetailData
                 { Resource = res, MyRating = res.MapRatings.SingleOrDefault(x => x.AccountID == Global.AccountID) ?? new MapRating() };

      // load map info from disk - or used cached copy if its in memory
      var cachedEntry = HttpContext.Application["mapinfo_" + res.ResourceID] as Map;
      if (cachedEntry != null) data.MapInfo = cachedEntry;
      else
      {
        var path = Server.MapPath("~/Resources/") + res.MetadataName;
        if (System.IO.File.Exists(path))
        {
          data.MapInfo = (Map)new XmlSerializer(typeof(Map)).Deserialize(new MemoryStream(System.IO.File.ReadAllBytes(path).Decompress()));
          HttpContext.Application["mapinfo_" + res.ResourceID] = data.MapInfo;
        }
      }

      if (res.ForumThread != null) {
        data.Posts = (from p in res.ForumThread.ForumPosts.OrderByDescending(x => x.Created)
                      let userRating = res.MapRatings.SingleOrDefault(x => x.AccountID == p.AuthorAccountID)
                      select
                        new MapPost { Created = p.Created, Author = p.Account, Text = p.Text, Rating = userRating != null ? (int?)userRating.Rating : null });
      } else data.Posts = new List<MapPost>();

      return data;
    }
    

    public class MapIndexData
    {
      public IQueryable<Resource> Latest;
      public IQueryable<Resource> TopRated;
      public IQueryable<Resource> LastComments;
      public IQueryable<Resource> MostDownloads;
    }


    public class MapDetailData
    {
      public Map MapInfo;
      public MapRating MyRating;
      public IEnumerable<MapPost> Posts;
      public Resource Resource;
    }

    public class MapPost
    {
      public Account Author;
      public DateTime Created;
      public int? Rating;
      public string Text;
    }
  }
}