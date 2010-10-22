using System;
using System.Linq;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{
	public class MissionsController: Controller
	{
		//
		// GET: /Missions/
		
		public ActionResult Index()
		{
			var db = new ZkDataContext();
			return
				View(
					new MissionsIndexData()
					{
						LastUpdated = db.Missions.OrderByDescending(x => x.ModifiedTime),
						MostPopular = db.Missions.OrderByDescending(x => x.DownloadCount),
						LastCommented = db.Missions.OrderBy(x => x.Name)
					});
		}

		public ActionResult Img(int id)
		{
			var db = new ZkDataContext();
			return File(db.Missions.Single(x => x.MissionID == id).Image.ToArray(), "image/png");
		}

		public ActionResult Detail(int id)
		{
			throw new NotImplementedException();
		}
	}

	public class MissionsIndexData
	{
		public IQueryable<Mission> LastUpdated;
		public IQueryable<Mission> MostPopular;
		public IQueryable<Mission> LastCommented;
	}
}