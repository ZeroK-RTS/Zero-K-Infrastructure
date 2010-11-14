using System;
using System.Data.Linq.SqlClient;
using System.Linq;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{
  public class MapsController: Controller
  {
    //
    // GET: /Maps/

    public ActionResult Index(string search,
                              int? offset,
                              bool? ffa,
                              bool? assymetrical,
                              int? sea,
                              int? hills,
                              int? size,
                              bool? elongated,
                              bool? isDownloadable,
                              bool? special = false)
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

      if (isDownloadable == true) ret = ret.Where(x => x.ResourceContentFiles.Any(y => y.LinkCount > 0));
      else if (isDownloadable == false) ret = ret.Where(x => x.ResourceContentFiles.All(y => y.LinkCount <= 0));
      if (special.HasValue) ret = ret.Where(x => x.MapIsSpecial == special);
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

      if (!offset.HasValue) return View(ret);
      else
      {
        if (ret.Any()) return View("MapTileList", ret);
        else return Content("");
      }
    }
  }
}