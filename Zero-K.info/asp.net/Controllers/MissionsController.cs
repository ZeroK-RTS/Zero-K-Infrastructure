using System;
using System.Data.Linq.SqlClient;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.UI;
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
						MostPopular = db.Missions.Where(x=>!x.IsDeleted).OrderByDescending(x => x.MissionRunCount),
						LastCommented = null, //db.Missions.OrderBy(x => x.Name),
						SearchString = search,
						FetchInitialCount = FetchInitialCount,
						FetchTileCount = FetchTileCount
					});
		}

		[OutputCache(VaryByParam = "name", Duration = int.MaxValue, Location = OutputCacheLocation.Any)]
		public ActionResult File(string name)
		{
			var m = new ZkDataContext().Missions.Single(x => x.Name == name);
			return File(m.Mutator.ToArray(), "application/octet-stream", m.SanitizedFileName);
		}

		public ActionResult Detail(int id)
		{
			var mission = new ZkDataContext().Missions.Single(x => x.MissionID == id);
			return View("Detail", new MissionDetailData
			                      {
															Mission = mission,
															TopScores = mission.MissionScores.OrderByDescending(x=>x.Score).Take(10).AsQueryable()
			                      });
		}


		static IQueryable<Mission> FilterMissions(IQueryable<Mission> ret, string search, int? offset = null)
		{
			ret = ret.Where(x => !x.IsDeleted);
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

		public ActionResult Script(int id)
		{
			var m = new ZkDataContext().Missions.Single(x => x.MissionID == id);
			return File(Encoding.UTF8.GetBytes(m.Script), "application/octet-stream", "script.txt");
		}

		[Authorize(Roles = "admin")]
		public ActionResult Delete(int id)
		{
			var db = new ZkDataContext();
			db.Missions.First(x => x.MissionID == id).IsDeleted = true;
			db.SubmitChanges();
			return RedirectToAction("Index");
		}

		public ActionResult Rate(int id, int difficulty, int rating)
		{
			if (!Global.IsAccountAuthorized) return Content("Not logged in!");
			else
			{
				var db = new ZkDataContext();
				var rat = db.Ratings.SingleOrDefault(x => x.MissionID == id && x.AccountID == Global.Account.AccountID);
				if (rat == null)
				{
					rat = new Rating();
					db.Ratings.InsertOnSubmit(rat);
				}
				rat.MissionID = id;
				rat.AccountID = Global.Account.AccountID;
				rat.Difficulty = difficulty;
				rat.Rating1 = rating;
				db.SubmitChanges();

				var mis = db.Missions.Single(x => x.MissionID == id);
				var ratings = mis.Ratings.OrderBy(x => x.Rating1).Select(x=>x.Rating1);
				var difficulties = mis.Ratings.OrderBy(x => x.Difficulty).Select(x => x.Difficulty);
				mis.Rating = ratings.Skip(ratings.Count()/2).FirstOrDefault();
				mis.Difficulty = difficulties.Skip(difficulties.Count() / 2).FirstOrDefault();
				db.SubmitChanges();

				var mission = new ZkDataContext().Missions.Single(x => x.MissionID == id);
				return View("Detail", new MissionDetailData
				{
					Mission = mission,
					TopScores = mission.MissionScores.OrderByDescending(x => x.Score).Take(10).AsQueryable()
				});

			}
		}
	}

	public class MissionDetailData
	{
		public Mission Mission;
		public IQueryable<MissionScore> TopScores;
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