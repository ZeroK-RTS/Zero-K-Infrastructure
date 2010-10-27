using System;
using System.Data.Linq.SqlClient;
using System.Linq;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{
	public class MissionsController: Controller
	{

		const int FetchTileCount = 20;
		const int FetchInitialCount = 20;
		//
		// GET: /Missions/
		public ActionResult Index(string search)
		{
			var db = new ZkDataContext();
			return
				View(
					new MissionsIndexData()
					{
						LastUpdated = FilterMissions(db.Missions, search).Take(FetchInitialCount),
						MostPopular = db.Missions.OrderByDescending(x => x.Resources.DownloadCount),
						LastCommented = db.Missions.OrderBy(x => x.Name),
						SearchString = search,
						FetchInitialCount = FetchInitialCount,
						FetchTileCount = FetchTileCount
					});
		}

		[OutputCache(VaryByParam = "name", Duration = int.MaxValue)]
		public ActionResult Img(string name)
		{
			var db = new ZkDataContext();
			return File(db.Missions.Single(x => x.Name == name).Image.ToArray(), "image/png");
		}

		[OutputCache(VaryByParam = "name", Duration = int.MaxValue)]
		public ActionResult File(string name)
		{
			var m = new ZkDataContext().Missions.Single(x => x.Name == name);
			return File(m.Mutator.ToArray(), "application/octet-stream", m.SanitizedFileName);
		}

		public ActionResult Detail(int id)
		{
			return View("Detail", new ZkDataContext().Missions.Single(x => x.MissionID == id));
		}

		static IQueryable<Mission> FilterMissions(IQueryable<Mission> ret, string search, int? offset = null)
		{
			if (!string.IsNullOrEmpty(search)) ret = ret.Where(x => SqlMethods.Like(x.Name, '%' + search + '%') || SqlMethods.Like(x.Account.Name, '%' + search + '%'));
			ret = ret.OrderByDescending(x => x.ModifiedTime);
			if (offset != null) ret = ret.Skip(offset.Value);
			return ret;
		}

		public ActionResult TileList(string search, int? offset)
		{
			var db = new ZkDataContext();
			var mis = FilterMissions(db.Missions, search, offset).Take(FetchTileCount);
			if (mis.Any()) return PartialView("TileList", mis);
			else return Content("");
		}
	}

	public class MissionsIndexData
	{
		public IQueryable<Mission> LastUpdated;
		public IQueryable<Mission> MostPopular;
		public IQueryable<Mission> LastCommented;
		public string SearchString;
		public int FetchInitialCount;
		public int FetchTileCount;
	}
}