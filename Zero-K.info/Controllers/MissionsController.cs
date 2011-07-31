using System;
using System.Collections.Generic;
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
	  //
		// GET: /Missions/
    public ActionResult Index(string search, int? offset, bool? sp, bool? coop, bool? adversarial, bool? featured)
		{
			var db = new ZkDataContext();
      if (!offset.HasValue) return
				View(
					new MissionsIndexData()
					{
            Title = "Latest missions",
						LastUpdated = FilterMissions(db.Missions, search, featured).Take(Global.AjaxScrollCount),
						MostPlayed = db.Missions.Where(x => !x.IsDeleted && (!Global.IsLimitedMode || x.ModRapidTag.StartsWith("zk:") || x.Mod.StartsWith("Zero-K") || x.Mod.StartsWith("Complete"))).OrderByDescending(x => x.MissionRunCount),
						MostRating = db.Missions.Where(x => !x.IsDeleted && (!Global.IsLimitedMode || x.ModRapidTag.StartsWith("zk:") || x.Mod.StartsWith("Zero-K") || x.Mod.StartsWith("Complete"))).OrderByDescending(x => x.Rating),
						LastComments = db.Missions.Where(x => !x.IsDeleted && (!Global.IsLimitedMode || x.ModRapidTag.StartsWith("zk:") || x.Mod.StartsWith("Zero-K") || x.Mod.StartsWith("Complete"))).OrderByDescending(x => x.ForumThread.LastPost),
						SearchString = search,
					});

      else
      {
        var mis = FilterMissions(db.Missions, search, featured.Value, offset, sp, coop, adversarial).Take(Global.AjaxScrollCount);
        if (mis.Any()) return View("TileList", mis);
        else return Content("");
      }
		}

		[OutputCache(VaryByParam = "name", Duration = int.MaxValue, Location = OutputCacheLocation.Any)]
		public ActionResult File(string name)
		{
			var m = new ZkDataContext().Missions.Single(x => x.Name == name);
			return File(m.Mutator.ToArray(), "application/octet-stream", m.SanitizedFileName);
		}


		[Auth(Role = AuthRole.ZkAdmin)]
    public ActionResult ChangeFeaturedOrder(int id, float? featuredOrder, string script)
    {
      var db = new ZkDataContext();
      var mis = db.Missions.SingleOrDefault(x => x.MissionID == id);
      mis.FeaturedOrder = featuredOrder;
      if (mis.IsScriptMission && !string.IsNullOrEmpty(script)) mis.Script = script;
      db.SubmitChanges();

			int order = 1;
			if (featuredOrder.HasValue) {
				foreach (var m in db.Missions.Where(x => x.FeaturedOrder != null).OrderBy(x => x.FeaturedOrder)) {
					m.FeaturedOrder = order++;
				}
			}
			db.SubmitChanges();

      return RedirectToAction("Index");
    }

		public ActionResult Detail(int id)
		{
      var db = new ZkDataContext();
      var mission = db.Missions.Single(x => x.MissionID == id);
      mission.ForumThread.UpdateLastRead(Global.AccountID, false);
      db.SubmitChanges();

			return View("Detail",
			            new MissionDetailData
			            {
			            	Mission = mission,
			            	TopScores = mission.MissionScores.OrderByDescending(x => x.Score).AsQueryable(),
			            	MyRating = mission.Ratings.SingleOrDefault(x => x.AccountID == Global.AccountID) ?? new Rating(),
			            });

		}


		static IQueryable<Mission> FilterMissions(IQueryable<Mission> ret, string search, bool? featured, int? offset = null, bool? sp = null, bool? coop= null, bool? adversarial= null)
		{
      ret = ret.Where(x => !x.IsDeleted && (!Global.IsLimitedMode || x.ModRapidTag.StartsWith("zk:") || x.Mod.StartsWith("Zero-K") || x.Mod.StartsWith("Complete")));
      if (featured == true) ret = ret.Where(x => x.FeaturedOrder > 0);
			if (sp == false) ret = ret.Where(x => x.MaxHumans > 1);
			if (coop == false) ret = ret.Where(x => (x.MinHumans<=1 && sp==true) ||  x.MaxHumans > 1 && !x.IsCoop);
			if (adversarial == false) ret = ret.Where(x => (x.MinHumans<=1 && sp==true) || (x.MaxHumans > 1 && x.IsCoop));
			if (!string.IsNullOrEmpty(search)) ret = ret.Where(x => x.Name.Contains(search) || x.Account.Name.Contains(search));


      if (!Global.IsAccountAuthorized || Global.Account.LobbyTimeRank <= 3 || featured == true) ret = ret.OrderByDescending(x => -x.FeaturedOrder).ThenByDescending(x => x.ModifiedTime);
      else ret = ret.OrderByDescending(x => x.ModifiedTime);
      if (offset != null) ret = ret.Skip(offset.Value);

			return ret;
		}

		public ActionResult Script(int id)
		{
			var m = new ZkDataContext().Missions.Single(x => x.MissionID == id);
			return File(Encoding.UTF8.GetBytes(m.Script), "application/octet-stream", "script.txt");
		}

		[Auth(Role = AuthRole.ZkAdmin | AuthRole.LobbyAdmin)]
    public ActionResult Undelete(int id)
    {
      var db = new ZkDataContext();
      db.Missions.First(x => x.MissionID == id).IsDeleted = false;
      db.SubmitChanges();
      return RedirectToAction("Index");
    }


		[Auth(Role = AuthRole.ZkAdmin | AuthRole.LobbyAdmin)]
		public ActionResult Delete(int id)
		{
			var db = new ZkDataContext();
			db.Missions.First(x => x.MissionID == id).IsDeleted = true;
			db.SubmitChanges();
			return RedirectToAction("Index");
		}

		[Auth]
		public ActionResult Rate(int id, int? difficulty, int? rating)
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
				if (difficulty.HasValue) rat.Difficulty = difficulty;
				if (rating.HasValue) rat.Rating1 = rating;
				db.SubmitChanges();

				var mis = db.Missions.Single(x => x.MissionID == id);
				mis.Rating = (float?)mis.Ratings.Average(x => x.Rating1);
				mis.Difficulty = (float?)mis.Ratings.Average(x => x.Difficulty);
				db.SubmitChanges();
				
				return Content("");
		}
	}

	public class MissionDetailData
	{
		public Mission Mission;
		public IQueryable<MissionScore> TopScores;
		public Rating MyRating;
	}

	public class MissionsIndexData
	{
		public IQueryable<Mission> LastUpdated;
		public IQueryable<Mission> MostPlayed;
		public IQueryable<Mission> MostRating;
		public string SearchString;
		public IQueryable<Mission> LastComments;
	  public string Title;
	}
}