using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using PlasmaShared;
using ZeroKWeb.AppCode;
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
			sb.AppendFormat("<img src='http://zero-k.info/Resources/{0}' /><br/>", r.ThumbnailName);
			sb.AppendFormat("Rating: {0}", HtmlHelperExtensions.Stars(null, StarType.GreenStarSmall, r.MapRating).ToHtmlString());

			sb.Append("</span>");
			return sb.ToString();
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
			int id;
			var db = new ZkDataContext();
			switch (args[0])
			{
				case "mission":

					ret = GetMissionTooltip(int.Parse(args[1]));
					break;
				case "map":
					if (int.TryParse(args[1], out id)) ret = GetMapTooltip(id);
					else ret = GetMapTooltip(db.Resources.Single(x => x.InternalName == args[1]).ResourceID);
					break;
				case "thread":
					if (int.TryParse(args[1], out id)) ret = GetThreadTooltip(id);
					break;
				case "unlock":
					return PartialView("UnlockTooltip", db.Unlocks.Single(x => x.UnlockID == int.Parse(args[1])));

				case "polloption":
					return PartialView("~/Views/Poll/PollVoteList.cshtml", db.PollVotes.Where(x => x.OptionID == int.Parse(args[1])).Select(x=>x.Account).OrderByDescending(x=>x.Level));
				case "commander":
					ret = GetCommanderTooltip(int.Parse(args[1]));

					break;
			}
			return Content(ret);
		}

		public ActionResult Index()
		{
			var db = new ZkDataContext();

			var result = new IndexResult()
			             {
			             	Spotlight = SpotlightHandler.GetRandom(),
			             	Top10Players =
			             		db.Accounts.Where(x => x.SpringBattlePlayers.Any(y => y.SpringBattle.StartTime > DateTime.UtcNow.AddMonths(-1))).OrderByDescending(
			             			x => x.Elo).Take(10)
			             };

			result.LobbyStats = AuthServiceClient.GetLobbyStats();

			if (!Global.IsAccountAuthorized) result.NewThreads = db.ForumThreads.OrderByDescending(x => x.LastPost).Take(10).Select(x => new NewThreadEntry() { ForumThread = x });
			else
			{
				result.NewThreads = (from t in db.ForumThreads
				                     let read = t.ForumThreadLastReads.SingleOrDefault(x => x.AccountID == Global.AccountID)
				                     where read == null || t.LastPost > read.LastRead
				                     orderby t.LastPost descending
				                     select new NewThreadEntry { ForumThread = t, WasRead = read != null, WasWritten = read != null && read.LastPosted != null }).
					Take(10);
			}

			return View(result);
		}


		public ActionResult Logon(string login, string password, string referer)
		{
			var db = new ZkDataContext();

			var acc = db.Accounts.SingleOrDefault(x => x.Name == login);
			if (acc == null) return Content("Invalid login name");
			var hashed = Utils.HashLobbyPassword(password);
			acc = AuthServiceClient.VerifyAccountHashed(login, hashed);
			if (acc == null) return Content("Invalid password");
			else
			{
				Response.SetCookie(new HttpCookie(GlobalConst.LoginCookieName, login) { Expires = DateTime.Now.AddMonths(12) });
				Response.SetCookie(new HttpCookie(GlobalConst.PasswordHashCookieName, hashed) { Expires = DateTime.Now.AddMonths(12) });

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
			return Redirect(referer);
		}

		public ActionResult Sitemap()
		{
			var sb = new StringBuilder();
			var db = new ZkDataContext();

			foreach (var x in db.Missions) sb.AppendLine(Url.Action("Detail", "Missions", new { id = x.MissionID }, "http"));

			foreach (var x in db.Resources) sb.AppendLine(Url.Action("Detail", "Maps", new { id = x.ResourceID }, "http"));

			foreach (var x in db.ForumThreads) sb.AppendLine(Url.Action("Thread", "Forum", new { id = x.ForumThreadID }, "http"));

			foreach (var x in db.SpringBattles) sb.AppendLine(Url.Action("Detail", "Battles", new { id = x.SpringBattleID }, "http"));

			foreach (var x in db.Accounts.OrderByDescending(x=>x.LastLogin).Take(5000)) sb.AppendLine(Url.Action("Index", "Users", new { name = x.Name }, "http"));

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
			sb.AppendFormat("<h3>{0}</h3>", c.Name);
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
			return sb.ToString();
		}


		string GetThreadTooltip(int id)
		{
			var db = new ZkDataContext();
			var thread = db.ForumThreads.Single(x => x.ForumThreadID == id);
			ForumPost post = null;
			ForumThreadLastRead last;

			var postTitle = "Starting post ";
			if (Global.IsAccountAuthorized && (last = thread.ForumThreadLastReads.SingleOrDefault(x => x.AccountID == Global.AccountID)) != null)
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
		}

		public class NewThreadEntry
		{
			public ForumThread ForumThread;
			public bool WasRead;
			public bool WasWritten;
		}
	}
}