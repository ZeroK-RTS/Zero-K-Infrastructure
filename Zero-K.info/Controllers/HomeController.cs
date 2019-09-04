using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OpenId;
using DotNetOpenAuth.OpenId.Extensions.SimpleRegistration;
using DotNetOpenAuth.OpenId.RelyingParty;
using LobbyClient;
using PlasmaShared;
using Ratings;
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

        /// <summary>
        /// Gets the appropriate tooltip for an object, e.g. a <see cref="ForumThread"/> or a <see cref="Clan"/>
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
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

        public class CurrentLobbyStats
        {
            public int BattlesRunning;
            public int UsersFighting;
            public int UsersOnline;
            public int UsersDiscord;
        }


        static CurrentLobbyStats GetCurrentLobbyStats()
        {
            var ret = new CurrentLobbyStats();
            if (Global.Server != null)
            {
                ret.UsersOnline = Global.Server.ConnectedUsers.Count;

                foreach (var b in Global.Server.Battles.Values)
                {
                    if (b.IsInGame)
                    {
                        ret.BattlesRunning++;
                        ret.UsersFighting += b.NonSpectatorCount + b.SpectatorCount;
                    }
                }

                ret.UsersDiscord = Global.Server.GetDiscordUserCount();
            }

            return ret;
        }




        /// <summary>
        /// Go to home page; also updates news read dates
        /// </summary>
		public ActionResult Index()
		{
			var db = new ZkDataContext();

		    
            var result = new IndexResult()
			             {
			             	Spotlight = SpotlightHandler.GetRandom(),
			             	Top10Players = RatingSystems.GetRatingSystem(RatingCategory.MatchMaking).GetTopPlayers(10),
                            WikiRecentChanges = MediaWikiRecentChanges.LoadRecentChanges()
                        };

			result.LobbyStats =  MemCache.GetCached("lobby_stats", GetCurrentLobbyStats, 60*2);

			result.News = db.News.Where(x => x.Created < DateTime.UtcNow).OrderByDescending(x => x.Created);
			if (Global.Account != null) {
				result.Headlines =
					db.News.Where(
						x => x.Created < DateTime.UtcNow && x.HeadlineUntil != null && x.HeadlineUntil > DateTime.UtcNow && !x.ForumThread.ForumThreadLastReads.Any(y=>y.AccountID== Global.AccountID && y.LastRead != null)).
						OrderByDescending(x => x.Created).ToList();

				if (result.Headlines.Any())
				{
				    foreach (var h in result.Headlines) h.ForumThread.UpdateLastRead(Global.AccountID, false);

				    db.SaveChanges();
				}
			} else {
				result.Headlines = new List<News>();
			}


			var accessibleThreads = db.ForumThreads.Where(x => x.RestrictedClanID == null || x.RestrictedClanID == Global.ClanID);
            accessibleThreads = accessibleThreads.Where(x => x.ForumCategory.ForumMode != ForumMode.Archive);
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

			

			return View("HomeIndex",result);
		}


	    public ActionResult NotLoggedIn()
		{
			return View();
		}

        [AcceptVerbs(HttpVerbs.Post | HttpVerbs.Get)]
        public ActionResult Logon(string login, string password, string referer, string zklogin)
		{
		    if (!Global.Server.LoginChecker.VerifyIp(Request.UserHostAddress)) return Content("Too many login failures, access blocked");

		    var openid = new OpenIdRelyingParty();
            IAuthenticationResponse response = openid.GetResponse();

		    if (response != null) // return from steam openid 
		        return ProcessSteamOpenIDResponse(response);

		    if (string.IsNullOrEmpty(zklogin)) // steam login request
		        return RedirectToSteamOpenID(login, referer, openid);


		    // standard login
            var db = new ZkDataContext();
		    var loginUpper = login.ToUpper();
            var acc = db.Accounts.FirstOrDefault(x => x.Name == login) ?? db.Accounts.FirstOrDefault(x=>x.Name.ToUpper() == loginUpper);
            if (acc == null) return Content("Invalid login name");
            var hashed = Utils.HashLobbyPassword(password);
            
			acc = AuthServiceClient.VerifyAccountHashed(acc.Name, hashed);
		    if (acc != null)
		    {
		        FormsAuthentication.SetAuthCookie(acc.Name, true);
		        if (string.IsNullOrEmpty(referer)) referer = Url.Action("Index");
		        return Redirect(referer);
		    }
		    else
		    {
		        Trace.TraceWarning("Invalid login attempt for {0}", login);
		        Global.Server.LoginChecker.LogIpFailure(Request.UserHostAddress);
		        return Content("Invalid password");
		    }
		}

	    private ActionResult RedirectToSteamOpenID(string login, string referer, OpenIdRelyingParty openid)
	    {
            IAuthenticationRequest request=null;
	        int tries = 3;
            while (request == null && tries > 0)
                try
                {
                    tries--;
                    request = openid.CreateRequest(Identifier.Parse("https://steamcommunity.com/openid/"));
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Steam openid CreateRequest has failed: {0}", ex);
                }
	        if (request == null) return Content("Steam OpenID service is offline, cannot authorize, please try again later.");
	        if (!string.IsNullOrEmpty(referer)) request.SetCallbackArgument("referer", referer);
	        return request.RedirectingResponse.AsActionResultMvc5();
	    }

	    private ActionResult ProcessSteamOpenIDResponse(IAuthenticationResponse response)
	    {
	        switch (response.Status)
	        {
	            case AuthenticationStatus.Authenticated:
	                var steamIDStr = response.FriendlyIdentifierForDisplay.Split('/').LastOrDefault();
	                ulong steamID;
	                if (ulong.TryParse(steamIDStr, out steamID))
	                {
                        var referer = response.GetCallbackArgument("referer");
	                    using (var db = new ZkDataContext())
	                    {
	                        var acc = db.Accounts.FirstOrDefault(x => x.SteamID == steamID);
	                        if (acc != null)
	                        {
	                            FormsAuthentication.SetAuthCookie(acc.Name, true);
	                            if (string.IsNullOrEmpty(referer)) referer = Url.Action("Index");
	                            return Redirect(referer);
	                        }
	                        else return Content("Please download the game and create an account in-game first");
	                    }
	                }
	                break;
	            case AuthenticationStatus.Canceled:
	                return Content("Login was cancelled at the provider");
	            case AuthenticationStatus.Failed:
	                return Content("Login failed");
	        }
	        return View("HomeIndex");
	    }

	    public ActionResult Logout(string referer)
		{
			if (Global.IsAccountAuthorized)
			{
                FormsAuthentication.SignOut();
			}
            if (string.IsNullOrEmpty(referer)) referer = Url.Action("Index");
			return Redirect(referer);
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
			public IEnumerable<MediaWikiRecentChanges.MediaWikiEdit> WikiRecentChanges;
		}

		public class NewThreadEntry
		{
			public ForumThread ForumThread;
			public bool WasRead;
			public bool WasWritten;
		}
	}
}
