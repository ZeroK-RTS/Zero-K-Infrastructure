using System;
using System.Collections.Generic;
using System.Data.Linq.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class MapsController : Controller
    {
        //
        // GET: /Maps/

        public ActionResult Index(string search, int? offset)
        {
          var db = new ZkDataContext();
          if (!offset.HasValue) return View(FilterMaps(db.Resources.Where(x=>x.TypeID== ZkData.ResourceType.Map).Take(Global.AjaxScrollCount), search, offset));
          else {
            var mis = FilterMaps(db.Resources.Where(x => x.TypeID == ZkData.ResourceType.Map), search, offset).Take(Global.AjaxScrollCount);
            if (mis.Any()) return View("MapTileList", mis);
            else return Content("");
          }
        }

        static IQueryable<Resource> FilterMaps(IQueryable<Resource> ret, string search, int? offset = null)
        {
          ret = ret.Where(x=>x.ResourceContentFiles.Any(y=>y.LinkCount>0));
          if (!string.IsNullOrEmpty(search)) ret = ret.Where(x => SqlMethods.Like(x.InternalName, '%' + search + '%'));
          ret = ret.OrderByDescending(x => x.ResourceID);
          if (offset != null) ret = ret.Skip(offset.Value);
          return ret;
        }


    }
}
