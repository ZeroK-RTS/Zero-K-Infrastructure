using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using ZeroKWeb;
using ZkData;

namespace ZeroKWeb.Controllers
{

	public class HomeController: Controller
	{
	    //
		// GET: /Home/
		public static string GetMapTooltip(int id)
		{
			var db = new ZkDataContext();
			var sb = new StringBuilder();
			var r = db.Resources.Single(x => x.ResourceID == id);
			sb.Append("<span>");
			sb.AppendFormat("{0}<br/>", r.InternalName);
			sb.AppendFormat("by {0}<br/>", r.AuthorName);
			if (r.MapIsFfa == true) sb.AppendFormat("<img src='/img/map_tags/ffa.png' class='icon32'  />");
			if (r.MapWaterLevel > 0) sb.AppendFormat("<img src='/img/map_tags/sea{0}.png' class='icon32'  />", r.MapWaterLevel);
			if (r.MapHills > 0) sb.AppendFormat("<img src='/img/map_tags/hill{0}.png' class='icon32' />", r.MapHills);
			if (r.MapIsSpecial == true) sb.AppendFormat("<img src='/img/map_tags/special.png' class='icon32' />");
			if (r.MapIsAssymetrical == true) sb.AppendFormat("<img src='/img/map_tags/assymetrical.png' class='icon32' />");
			sb.Append("<br/>");
			sb.AppendFormat("<img src='/Resources/{0}' /><br/>", r.ThumbnailName);
			sb.AppendFormat("Rating: {0}", HtmlHelperExtensions.Stars(null, StarType.GreenStarSmall, r.MapRating).ToHtmlString());

			sb.Append("</span>");
			
			return sb.ToString();
		}

		public ActionResult SwitchSkin()
		{
			var newValue = "";
			if (Request["minimalDesign"] == "1") newValue = "0";
			else newValue = "1";
			Response.Cookies.Add(new HttpCookie("minimalDesign", newValue) { Expires = DateTime.Now.AddMonths(6)});
			return RedirectToAction("Index");
		}


        public ActionResult Download() {

            return View();
        }

	    public static string GetMissionTooltip(int id)
		{
			var db = new ZkDataContext();
			var sb = new StringBuilder();
			var mis = db.Missions.Single(x => x.MissionID == id);

			sb.Append("<span>");
			sb.AppendFormat("{0}<br/>---<br/>", HttpUtility.HtmlEncode(mis.Description ?? "").Replace("\n", "<br/>"));
			sb.AppendFormat("Players: {0}<br/>", mis.MinToMaxHumansString);
			sb.AppendFormat("<small>{0}</small><br/>", string.Join(",", mis.GetPseudoTags()));
			sb.AppendFormat("Map: {0}<br/>", mis.Map);
			sb.AppendFormat("Game: {0}<br/>", mis.Mod ?? mis.ModRapidTag);
			sb.AppendFormat("Played: {0} times<br/>", mis.MissionRunCount);
			sb.AppendFormat("Rated: {0} times<br/>", mis.Ratings.Count);
			sb.AppendFormat("Comments: {0}<br/>", mis.ForumThread != null ? mis.ForumThread.ForumPosts.Count : 0);
			sb.Append("</span>");

			return sb.ToString();
		}

		[NoCache]
		public ActionResult GetTooltip(string key)
		{
			var args = key.Split(new char[] { '$' }, StringSplitOptions.RemoveEmptyEntries);
			var ret = "";
			int id = 0;
		    string sid = null;
			var db = new ZkDataContext();
		    if (args.Length > 1) {
		        int.TryParse(args[1], out id);
		        sid = args[1];
		    }
            
			switch (args[0])
			{
				case "mission":

					ret = GetMissionTooltip(id);
					break;
				case "map":
					if (id > 0) ret = GetMapTooltip(id);
					else ret = GetMapTooltip(db.Resources.Single(x => x.InternalName == sid).ResourceID);
					break;
				case "thread":
					ret = GetThreadTooltip(id);
					break;
				case "unlock":
					return PartialView("UnlockTooltip", db.Unlocks.Single(x => x.UnlockID == id));

				case "polloption":
					return PartialView("~/Views/Poll/PollVoteList.cshtml", db.PollVotes.Where(x => x.OptionID == id).Select(x=>x.Account).OrderByDescending(x=>x.Level).ToList());
				case "commander":
					ret = GetCommanderTooltip(id);
					break;

				case "planet":
					return PartialView("PlanetTooltip", db.Planets.Single(x => x.PlanetID == id));
                case "campaignPlanet":
                    return PartialView("PlanetTooltipCampaign", db.CampaignPlanets.Single(x => x.PlanetID == id));

				case "planetInfluence":
					return PartialView("InfluenceListShort", db.Planets.Single(x => x.PlanetID == id).PlanetFactions);

				case "planetDropships":
					return PartialView("PlanetDropships", db.Planets.Single(x => x.PlanetID == id));

                case "user":
                    return PartialView("UserTooltip", db.Accounts.Single(x => x.AccountID == id));
                case "clan":
			        return PartialView("~/Views/Clans/Tooltip.cshtml", db.Clans.Single(x => x.ClanID == id));
                case "faction":
			        return PartialView("~/Views/Factions/FactionTooltip.cshtml", db.Factions.Single(x => x.FactionID == id));
                case "treaty":
                    return PartialView("~/Views/Shared/DisplayTemplates/FactionTreaty.cshtml", db.FactionTreaties.Single(x => x.FactionTreatyID == id));
                case "structuretype":
			        return PartialView("~/Views/Shared/DisplayTemplates/StructureType.cshtml",
			                           db.StructureTypes.Single(x => x.StructureTypeID == id));
                case "forumVotes":
                    return PartialView("~/Views/Forum/ForumVotesForPost.cshtml", db.ForumPosts.Single( x => x.ForumPostID == id));
			}
			return Content(ret);
		}

		public ActionResult Index()
		{
            if (Request[GlobalConst.ASmallCakeCookieName] != null)
            {
                return RedirectToAction("Index", new {});
            }
			var db = new ZkDataContext();

		    var prevMonth = DateTime.UtcNow.AddMonths(-1);
			var result = new IndexResult()
			             {
			             	Spotlight = SpotlightHandler.GetRandom(),
			             	Top10Players =
			             		db.Accounts.Where(x => x.SpringBattlePlayers.Any(y => y.SpringBattle.StartTime > prevMonth)).OrderByDescending(
			             			x => x.Elo1v1).Take(10)
			             };

			result.LobbyStats = AuthServiceClient.GetLobbyStats();
			result.News = db.News.Where(x => x.Created < DateTime.UtcNow).OrderByDescending(x => x.Created);
			if (Global.Account != null) {
				result.Headlines =
					db.News.Where(
						x => x.Created < DateTime.UtcNow && x.HeadlineUntil != null && x.HeadlineUntil > DateTime.UtcNow && (Global.Account.LastNewsRead == null || ( x.Created > Global.Account.LastNewsRead))).
						OrderByDescending(x => x.Created);

				if (result.Headlines.Any()) {
					db.Accounts.Single(x => x.AccountID == Global.AccountID).LastNewsRead = DateTime.UtcNow;
					db.SubmitChanges();
				}
			} else {
				result.Headlines = new List<News>();
			}


			var accessibleThreads = db.ForumThreads.Where(x => x.RestrictedClanID == null || x.RestrictedClanID == Global.ClanID);
			if (!Global.IsAccountAuthorized) result.NewThreads = accessibleThreads.OrderByDescending(x => x.LastPost).Take(10).Select(x => new NewThreadEntry() { ForumThread = x });
			else
			{
				result.NewThreads = (from t in accessibleThreads
				                     let read = t.ForumThreadLastReads.FirstOrDefault(x => x.AccountID == Global.AccountID)
                                     let readForum = t.ForumCategory.ForumLastReads.FirstOrDefault(x=> x.AccountID == Global.AccountID)
				                     where (read == null || t.LastPost > read.LastRead) && (readForum == null || t.LastPost > readForum.LastRead)
				                     orderby t.LastPost descending
				                     select new NewThreadEntry { ForumThread = t, WasRead = read != null, WasWritten = read != null && read.LastPosted != null }).
					Take(10);
			}

			return View(result);
		}


		
		public ActionResult NotLoggedIn()
		{
			return View();
		}

		public ActionResult Logon(string login, string password, string referer)
		{
			var db = new ZkDataContext();

			var acc = db.Accounts.FirstOrDefault(x => x.Name == login);    // FIXME: might want to just not allow duplicate names to happen in the first place
			if (acc == null) return Content("Invalid login name");
			var hashed = Utils.HashLobbyPassword(password);
			acc = AuthServiceClient.VerifyAccountHashed(login, hashed);
			if (acc == null) return Content("Invalid password");
			else
			{
                // todo replace with safer permanent cookie
				Response.SetCookie(new HttpCookie(GlobalConst.LoginCookieName, login) { Expires = DateTime.Now.AddMonths(12) });
				Response.SetCookie(new HttpCookie(GlobalConst.PasswordHashCookieName, hashed) { Expires = DateTime.Now.AddMonths(12) });

                FormsAuthentication.SetAuthCookie(acc.Name, false);

                if (string.IsNullOrEmpty(referer)) referer = Url.Action("Index");
				return Redirect(referer);
			}
		}

		public ActionResult Logout(string referer)
		{
			if (Global.IsAccountAuthorized)
			{
				Response.SetCookie(new HttpCookie(GlobalConst.LoginCookieName, "") { Expires = DateTime.Now.AddMinutes(2) });
				Response.SetCookie(new HttpCookie(GlobalConst.PasswordHashCookieName, "") { Expires = DateTime.Now.AddMinutes(2) });
			}
            if (string.IsNullOrEmpty(referer)) referer = Url.Action("Index");
			return Redirect(referer);
		}

        [OutputCache(Duration = 3600*24)]
		public ActionResult Sitemap()
		{
			var sb = new StringBuilder();
			var db = new ZkDataContext();

			foreach (var x in db.Missions) sb.AppendLine(Url.Action("Detail", "Missions", new { id = x.MissionID }, "http"));

			foreach (var x in db.Resources.Where(x=>x.TypeID == ResourceType.Map)) sb.AppendLine(Url.Action("Detail", "Maps", new { id = x.ResourceID }, "http"));

			foreach (var x in db.ForumThreads) sb.AppendLine(Url.Action("Thread", "Forum", new { id = x.ForumThreadID }, "http"));

            var wikiIndex = new WebClient().DownloadString("http://zero-k.googlecode.com/svn/wiki/");
			var matches = Regex.Matches(wikiIndex, "\"([^\"]+)\"");
			foreach (Match m in matches)
			{
				if (m.Groups[1].Value.EndsWith(".wiki"))
				{
					var name = m.Groups[1].Value;
					name = name.Substring(0, name.Length - 5);

					sb.AppendLine(Url.Action("Index", "Wiki", new { node = name }, "http"));
				}
			}

			return Content(sb.ToString());
		}

		string GetCommanderTooltip(int commanderID)
		{
			var db = new ZkDataContext();
			var sb = new StringBuilder();
			var c = db.Commanders.Single(x => x.CommanderID == commanderID);
			sb.AppendLine("<span>");
			sb.AppendFormat("<h3>{0}</h3>", System.Web.HttpContext.Current.Server.HtmlEncode(c.Name));
			sb.AppendFormat("<img src='{0}'/><br/>", c.Unlock.ImageUrl);
			foreach (var slots in c.CommanderModules.GroupBy(x => x.CommanderSlot.MorphLevel).OrderBy(x => x.Key))
			{
				sb.AppendFormat("<b>Morph {0}:</b><br/>", slots.Key);
				foreach (var module in slots.OrderBy(x => x.SlotID))
				{
					sb.AppendFormat("<img src='{0}' width='20' height='20'><span style='color:{2};'>{1}</span><br/>",
					                module.Unlock.ImageUrl,
					                module.Unlock.Name,
					                module.Unlock.LabelColor);
				}
			}
            foreach (var decSlots in c.CommanderDecorations.ToList())
            {
				// TBD
            }
			return sb.ToString();
		}


		string GetThreadTooltip(int id)
		{
			var db = new ZkDataContext();
			var thread = db.ForumThreads.Single(x => x.ForumThreadID == id);
			ForumPost post = null;
			ForumThreadLastRead last;
            ForumLastRead lastForum;

			if (thread.RestrictedClanID != null && thread.RestrictedClanID != Global.ClanID)
			{
				return "<span>This is a secret clan thread :-)</span>";
			}

			var postTitle = "Starting post ";
            if (Global.IsAccountAuthorized)
            {
                if ((last = thread.ForumThreadLastReads.SingleOrDefault(x => x.AccountID == Global.AccountID)) != null)
                {
                    if (last.LastRead < thread.LastPost)
                    {
                        postTitle = "First unread post ";
                        post = thread.ForumPosts.Where(x => x.Created > last.LastRead).OrderBy(x => x.ForumPostID).FirstOrDefault();
                    }
                    else
                    {
                        postTitle = "Last post ";
                        post = thread.ForumPosts.OrderByDescending(x => x.ForumPostID).FirstOrDefault();
                    }
                }
                else if ((lastForum = thread.ForumCategory.ForumLastReads.SingleOrDefault(x => x.AccountID == Global.AccountID)) != null)
                {
                    if (lastForum.LastRead < thread.LastPost)
                    {
                        postTitle = "First unread post ";
                        post = thread.ForumPosts.Where(x => x.Created > lastForum.LastRead).OrderBy(x => x.ForumPostID).FirstOrDefault();
                    }
                    else
                    {
                        postTitle = "Last post ";
                        post = thread.ForumPosts.OrderByDescending(x => x.ForumPostID).FirstOrDefault();
                    }
                }
            }
            else post = thread.ForumPosts.OrderBy(x => x.ForumPostID).FirstOrDefault();
			var sb = new StringBuilder();

			if (post != null)
			{
				sb.AppendFormat("{0} {1}, {2}", postTitle, HtmlHelperExtensions.PrintAccount(null, post.Account).ToHtmlString(), post.Created.ToAgoString());
				sb.AppendFormat("<br/><span>{0}</span><br/>", HtmlHelperExtensions.BBCode(null, post.Text).ToHtmlString());
			}
			sb.AppendFormat("<small>Thread by {0}, {1}</small>",
			                HtmlHelperExtensions.PrintAccount(null, thread.AccountByCreatedAccountID).ToHtmlString(),
			                thread.Created.ToAgoString());
			return sb.ToString();
		}

		public class IndexResult
		{
			public CurrentLobbyStats LobbyStats;
			public IQueryable<NewThreadEntry> NewThreads;
			public SpotlightHandler.UnitSpotlight Spotlight;
			public IEnumerable<Account> Top10Players;
			public IEnumerable<News> News;
			public IEnumerable<News> Headlines;
		}

		public class NewThreadEntry
		{
			public ForumThread ForumThread;
			public bool WasRead;
			public bool WasWritten;
		}
	}
}