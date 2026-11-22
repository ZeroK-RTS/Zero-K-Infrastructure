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
			sb.AppendFormat("<img src='/{0}/{1}' /><br/>", GlobalConst.ResourceFolder, r.ThumbnailName);
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
            if (Global.LobbyApi != null)
            {
                ret.UsersOnline = Global.LobbyApi.ConnectedUserCount;

                var battleStats = Global.LobbyApi.GetBattleStats();
                ret.BattlesRunning = battleStats.BattlesRunning;
                ret.UsersFighting = battleStats.UsersFighting;

                ret.UsersDiscord = Global.LobbyApi.GetDiscordUserCount();
            }

            return ret;
        }




        /// <summary>
        /// Go to home page; also updates news read dates
        /// </summary>
		[WritesOnGetByDesign("the headlines on the front page are marked read by being shown")]
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

        /// <summary>
        /// Sign-in, by GET. Steam and nothing else.
        ///
        /// **Logon used to be [AcceptVerbs("GET","POST")] across all three of its paths**, which made
        /// the password one a login CSRF: a hidden form - or a bare link - submitting the ATTACKER's
        /// credentials signs the visitor into the attacker's account, and everything they do next
        /// happens there. It is the one CSRF worth doing to somebody who is not logged in at all.
        ///
        /// The password path moved to the POST twin below, where an anti-forgery token is required.
        /// This half keeps what cannot ask for a token: Steam's OpenID return, which is a redirect
        /// from Steam and arrives as a GET carrying nothing of ours, and the request that starts it.
        ///
        /// **Nothing was signing in by GET.** zklogin comes from one place, the submit button in
        /// LoginBar.cshtml, and that is a POST. ZeroKLobby/BrowserInterop does fetch
        /// /Home/Logon?login=&amp;password= at startup, but it sends no zklogin, so it has always taken
        /// the Steam branch - the lobby client authenticates by planting the session-token cookie
        /// beside it, not through this.
        /// </summary>
        [HttpGet]
        public ActionResult Logon(string login, string password, string referer, string zklogin)
		{
            // Refused rather than quietly treated as a Steam request: a GET carrying credentials is
            // either the old behaviour being relied on somewhere this says it is not, or it is the
            // attack. Both are worth seeing rather than redirecting away.
            if (!string.IsNullOrEmpty(zklogin))
                return Content("Sign in by submitting the login form, not by following a link.");

            return LogonCore(login, password, referer, null);
		}

        /// <summary>
        /// The same sign-in by POST, with the anti-forgery token that closes the hole.
        ///
        /// [ActionName] so both halves answer on /Home/Logon and the verb chooses between them - the
        /// form's action, and the return_to handed to Steam, are unchanged.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Logon")]
        public ActionResult LogonPost(string login, string password, string referer, string zklogin)
		{
            return LogonCore(login, password, referer, zklogin);
		}

        /// <summary>Private: a public method on a controller is an action, which is how CreateLink got out.</summary>
        private ActionResult LogonCore(string login, string password, string referer, string zklogin)
		{
		    // PHASE 1: the login rate limiter counts failures in the LOBBY SERVER's memory, so
		    // this line is sign-in depending on the two processes being one. The null-conditional
		    // changes nothing where a server is attached - `null == false` is false, so a running
		    // server still blocks exactly as before - and lets the .NET 9 port, which has no server
		    // yet, serve the page. It does mean the port has NO login rate limiting; see
		    // ZkData/EFCORE-MIGRATION.md, which lists it as a blocker rather than a detail.
		    if (Global.LobbyApi?.VerifyIp(Request.UserHostAddressCompat()) == false) return Content("Too many login failures, access blocked");

		    // Steam sign-in goes through a twin pair - DotNetOpenAuth here, the protocol
		    // written out on .NET 9, which has no DotNetOpenAuth. See AppCode/SteamOpenId.cs.
		    var steam = SteamOpenId.TryCompleteLogin(this);

		    if (steam != null) // return from steam openid 
		        return ProcessSteamOpenIDResponse(steam);

		    if (string.IsNullOrEmpty(zklogin)) // steam login request
		        return RedirectToSteamOpenID(login, referer);


		    // standard login
            var db = new ZkDataContext();
		    var loginUpper = login.ToUpper();
            var acc = db.Accounts.FirstOrDefault(x => x.Name == login) ?? db.Accounts.FirstOrDefault(x=>x.Name.ToUpper() == loginUpper);
            if (acc == null) return Content("Invalid login name");
            var hashed = Utils.HashLobbyPassword(password);
            
			acc = AuthServiceClient.VerifyAccountHashed(acc.Name, hashed);
		    if (acc != null)
		    {
		        this.SignInCompat(acc);
		        if (string.IsNullOrEmpty(referer)) referer = Url.Action("Index");
		        return Redirect(referer);
		    }
		    else
		    {
		        Trace.TraceWarning("Invalid login attempt for {0}", login);
		        Global.LobbyApi?.LogIpFailure(Request.UserHostAddressCompat());
		        return Content("Invalid password");
		    }
		}

	    private ActionResult RedirectToSteamOpenID(string login, string referer)
	    {
	        return SteamOpenId.BeginLogin(this, referer)
	               ?? Content("Steam OpenID service is offline, cannot authorize, please try again later.");
	    }

	    private ActionResult ProcessSteamOpenIDResponse(SteamOpenIdResult steam)
	    {
	        switch (steam.Status)
	        {
	            case SteamOpenIdStatus.Authenticated:
	                var referer = steam.Referer;
	                using (var db = new ZkDataContext())
	                {
	                    var acc = db.Accounts.FirstOrDefault(x => x.SteamID == steam.SteamID);
	                    if (acc != null)
	                    {
	                        this.SignInCompat(acc);
	                        if (string.IsNullOrEmpty(referer)) referer = Url.Action("Index");
	                        return Redirect(referer);
	                    }
	                    else return Content("Please download the game and create an account in-game first");
	                }
	            case SteamOpenIdStatus.Canceled:
	                return Content("Login was cancelled at the provider");
	            case SteamOpenIdStatus.Failed:
	                Trace.TraceWarning("Steam sign-in failed: {0}", steam.FailureReason);
	                return Content("Login failed");
	        }
	        return View("HomeIndex");
	    }

	    public ActionResult Logout(string referer)
		{
			if (Global.IsAccountAuthorized)
			{
                this.SignOutCompat();
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
