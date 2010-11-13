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
    public ActionResult Index(string search, int? offset, bool? sp, bool? coop, bool? adversarial)
		{
			var db = new ZkDataContext();
      if (!offset.HasValue) return
				View(
					new MissionsIndexData()
					{
						LastUpdated = FilterMissions(db.Missions, search).Take(Global.AjaxScrollCount),
						MostPlayed = db.Missions.Where(x=>!x.IsDeleted).OrderByDescending(x => x.MissionRunCount),
						MostRating =db.Missions.Where(x=>!x.IsDeleted).OrderByDescending(x => x.Rating),
						LastComments = db.Missions.Where(x=>!x.IsDeleted).OrderByDescending(x=>x.ForumThread.LastPost),
						SearchString = search,
					});

      else
      {
        var mis = FilterMissions(db.Missions, search, offset, sp, coop, adversarial).Take(Global.AjaxScrollCount);
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

		public class MissionPost {
			public DateTime Created { get; set; }
			public Account Author { get; set; }
			public string Text { get; set; }
			public int? Rating { get; set; }
			public int? Difficulty { get; set; }
		}

		public ActionResult Detail(int id)
		{
			var mission = new ZkDataContext().Missions.Single(x => x.MissionID == id);
			return View("Detail",
			            new MissionDetailData
			            {
			            	Mission = mission,
			            	TopScores = mission.MissionScores.OrderByDescending(x => x.Score).Take(10).AsQueryable(),
			            	MyRating = mission.Ratings.SingleOrDefault(x => x.AccountID == Global.AccountID) ?? new Rating(),
			            	Posts = (from p in mission.ForumThread.ForumPosts.OrderByDescending(x => x.Created)
			            	         let userRating = mission.Ratings.SingleOrDefault(x => x.AccountID == p.AuthorAccountID)
			            	         select
			            	         	new MissionPost
			            	         	{
			            	         		Created = p.Created,
			            	         		Author = p.Account,
			            	         		Text = p.Text,
			            	         		Rating = userRating != null ? userRating.Rating1 : null,
			            	         		Difficulty = userRating != null ? userRating.Difficulty : null
			            	         	})
			            });

		}


		static IQueryable<Mission> FilterMissions(IQueryable<Mission> ret, string search, int? offset = null, bool? sp = null, bool? coop= null, bool? adversarial= null)
		{
			ret = ret.Where(x => !x.IsDeleted);
			if (sp == false) ret = ret.Where(x => x.MaxHumans > 1);
			if (coop == false) ret = ret.Where(x => (x.MinHumans<=1 && sp==true) ||  x.MaxHumans > 1 && !x.IsCoop);
			if (adversarial == false) ret = ret.Where(x => (x.MinHumans<=1 && sp==true) || (x.MaxHumans > 1 && x.IsCoop));
			if (!string.IsNullOrEmpty(search)) ret = ret.Where(x => SqlMethods.Like(x.Name, '%' + search + '%') || SqlMethods.Like(x.Account.Name, '%' + search + '%'));
			ret = ret.OrderByDescending(x => x.ModifiedTime);
      if (offset != null) ret = ret.Skip(offset.Value);

			return ret;
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

		public ActionResult Rate(int id, int? difficulty, int? rating)
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
				if (difficulty.HasValue) rat.Difficulty = difficulty;
				if (rating.HasValue) rat.Rating1 = rating;
				db.SubmitChanges();

				var mis = db.Missions.Single(x => x.MissionID == id);
/*			var ratings = mis.Ratings.OrderBy(x => x.Rating1).Select(x=>x.Rating1);
				var difficulties = mis.Ratings.OrderBy(x => x.Difficulty).Select(x => x.Difficulty);
				mis.Rating = ratings.Skip(ratings.Count()/2).FirstOrDefault();
				mis.Difficulty = difficulties.Skip(difficulties.Count() / 2).FirstOrDefault();*/

				mis.Rating = (float?)mis.Ratings.Average(x => x.Rating1);
				mis.Difficulty = (float?)mis.Ratings.Average(x => x.Difficulty);
				db.SubmitChanges();
				
				return Content("");
			}
		}
	}

	public class MissionDetailData
	{
		public Mission Mission;
		public IQueryable<MissionScore> TopScores;
		public Rating MyRating;
		public IEnumerable<MissionsController.MissionPost> Posts;
	}

	public class MissionsIndexData
	{
		public IQueryable<Mission> LastUpdated;
		public IQueryable<Mission> MostPlayed;
		public IQueryable<Mission> MostRating;
		public string SearchString;
		public IQueryable<Mission> LastComments;
	}
}