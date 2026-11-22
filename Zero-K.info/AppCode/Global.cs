using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Mime;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc.Ajax;
using System.Web.Routing;
using AutoRegistrator;
using JetBrains.Annotations;
using LobbyClient;
using PlasmaShared;
using ZkData;
using ZkLobbyServer;
using ZkLobbyServer.Api;
using Ratings;

namespace ZeroKWeb
{
    public static class Global
    {
        public const int AjaxScrollCount = 40;
        
        public static Account Account
        {
            get
            {
                if (HttpContext.Current == null) return null;
                return HttpContext.Current.User as Account;
            }
        }
        public static int AccountID
        {
            get
            {
                if (IsAccountAuthorized) return Account.AccountID;
                else return 0;
            }
        }

        public static Clan Clan
        {
            get
            {
                if (Account == null) return null;
                else return Account.Clan;
            }
        }
        public static int ClanID
        {
            get
            {
                if (IsAccountAuthorized && Clan != null) return Clan.ClanID;
                else return 0;
            }
        }
        public static int FactionID
        {
            get
            {
                if (IsAccountAuthorized && Account.Faction != null) return Account.FactionID ?? 0;
                else return 0;
            }
        }

        public static bool IsAccountAuthorized
        {
            get
            {
                if (HttpContext.Current == null) return false;
                return HttpContext.Current.User as Account != null;
            }
        }

        public static bool IsLobbyAccess
        {
            get { return HttpContext.Current.Request.Cookies[GlobalConst.LobbyAccessCookieName] != null; }
        }
        public static bool IsWebLobbyAccess
        {
            get { return HttpContext.Current.Session["weblobby"] != null; }
        }

        public static bool IsTourneyController
        {
            get { return IsAccountAuthorized && (Account?.AdminLevel >= AdminLevel.Moderator || Account?.IsTourneyController == true); }
        }
        public static bool IsModerator
        {
            get { return IsAccountAuthorized && Account?.AdminLevel>= AdminLevel.Moderator; }
        }
        public static bool IsSuperAdmin
        {
            get { return IsAccountAuthorized && Account?.AdminLevel >= AdminLevel.SuperAdmin; }
        }


        public static PayPalInterface PayPalInterface { get; private set; }
        //public static PlanetWarsMatchMaker PlanetWarsMatchMaker { get; private set; }
        /// <summary>
        /// The raw lobby server. Prefer <see cref="LobbyApi"/> - this stays public only for the
        /// Fixer tool, which still reads the PlanetWars matchmaker directly.
        /// </summary>
        public static ZkLobbyServer.ZkLobbyServer Server { get; private set; }

        /// <summary>
        /// The website's view of the lobby server, and now its only one. Every member of
        /// <see cref="ZkLobbyServer.ILobbyServerApi"/> can survive the server moving to its own
        /// process, which is what Zero-K.info/HOSTING.md is about. Null until the server starts.
        ///
        /// The type is the NARROW interface on purpose. It was ILobbyServerApiInProcess, which
        /// also exposed the live battle list, the live Battle objects and the server itself; now
        /// that nothing here needs those, declaring the narrow type is what stops them coming
        /// back. Reaching for one is a compile error in this build, not just in the .NET 9 port.
        ///
        /// The object behind it is still an InProcessLobbyServerApi, and the server still runs in
        /// this process. What changed is that the website can no longer tell.
        /// </summary>
        public static ZkLobbyServer.ILobbyServerApi LobbyApi { get; private set; }

        public static ServerRunner ZkServerRunner { get; private set; }
        public static ForumPostCache ForumPostCache { get; private set; }= new ForumPostCache();

        public static AutoRegistrator AutoRegistrator { get; private set; }
        

        public static string MapPath(string virtualPath)
        {
            if (HttpContext.Current != null) return HttpContext.Current.Server.MapPath(virtualPath);
            else {
                try {
                    return HostingEnvironment.MapPath(virtualPath);
                } catch {
                    return HttpRuntime.AppDomainAppPath + virtualPath.Replace("~", string.Empty).Replace('/', '\\');
                }
            }
        }

        private static int isStarted = 0;

        public static void StartApplication(MvcApplication mvcApplication)
        {
            GlobalConst.OverrideContentServiceClient(new ContentServiceImplementation()); // set to directly call content service instead going through json web request
            
            if (Interlocked.Exchange(ref isStarted, 1) == 1) return; // prevent double start

            var listener = new ZkServerTraceListener();
            Trace.Listeners.Add(listener);
            Trace.TraceInformation("Starting Zero-K.info web and application");

            // Says that THIS build reports who calls the two deprecated WCF endpoints.
            //
            // Without it their silence in LogEntries means either "nobody calls them" or "this
            // build never reported", and those lead to opposite decisions - the second one to
            // deleting an endpoint that mission editors still publish through. The line above
            // cannot serve: it predates the instrumentation and an old build writes it too.
            //
            // ZkData.Core -- legacy-callers reads it back and refuses to conclude without it.
            Trace.TraceInformation(
                LegacyCallAudit.WatchMarker + ": MissionService.svc and ContentService.svc report their callers");

            GlobalConst.SiteDiskPath = MapPath("~");
            
            AwardCalculator = new AwardCalculator();
            AwardCalculator.RecomputeNow();

            var sitePath = mvcApplication.Server.MapPath("~");

            // Phase 1: the lobby server runs here, or somewhere else.
            //
            // Setting the LobbyApiUrl MiscVar is the entire switch. Unset - which is every
            // deployment that has not been changed on purpose - means this process starts the
            // server as it always has, and nothing below behaves differently.
            //
            // Set, and this process starts NO lobby server. That is the point: two servers
            // against one database would both accept logins and both run PlanetWars turns.
            // Server and ZkServerRunner stay null, and the things that use them directly -
            // the trace listener and the tourney console - are the remaining in-process
            // dependencies, which is why ILobbyServerApiInProcess still exists.
            var lobbyApiUrl = MiscVar.GetValue(LobbyApiConfiguration.UrlKey);
            if (LobbyApiConfiguration.IsRemote(lobbyApiUrl))
            {
                Trace.TraceInformation("Using a lobby server at {0}", lobbyApiUrl);
                LobbyApi = LobbyApiConfiguration.CreateClient(
                    lobbyApiUrl,
                    MiscVar.GetValue(LobbyApiConfiguration.SecretKey),
                    LobbyApiConfiguration.AllowInsecureTransport(
                        MiscVar.GetValue(LobbyApiProtocol.AllowInsecureKey)));
            }
            else
            {
                ZkServerRunner = new ServerRunner(sitePath, new PlanetwarsEventCreator());
                Server = ZkServerRunner.ZkLobbyServer;
                LobbyApi = new ZkLobbyServer.InProcessLobbyServerApi(Server);

                Trace.TraceInformation("Starting lobby server");
                ZkServerRunner.Run();
            }

            ForumPostIndexer = new ForumPostIndexer();

            SteamDepotGenerator = new SteamDepotGenerator(sitePath, Path.Combine(sitePath, "..", "steamworks", "tools", "ContentBuilder", "content"));

            
            SetupPaypalInterface();


            // HACK Task.Factory.StartNew(() => SteamDepotGenerator.RunAll());

            Trace.TraceInformation("Starting autoregistrator");
            AutoRegistrator = new AutoRegistrator(MapPath("~"));
            AutoRegistrator.NewZkReleaseRegistered += (game, chobby) =>
            {
                SteamDepotGenerator.RunAll();
                LobbyApi.SetGame(game);
            };

            AutoRegistrator.RunMainAndMapSyncAsync();
        }

        public static SteamDepotGenerator SteamDepotGenerator { get; private set; }

        public static AwardCalculator AwardCalculator { get; private set; }

        public static ForumPostIndexer ForumPostIndexer { get; private set; }




        public static void StopApplication()
        {
            ZkServerRunner.Stop();
        }

        public static UrlHelper UrlHelper()
        {
            var httpContext = HttpContext.Current;

            RequestContext requestContext;
            if (httpContext == null) {
                var request = new HttpRequest("/", GlobalConst.BaseSiteUrl, "");
                var response = new HttpResponse(new StringWriter());
                httpContext = new HttpContext(request, response);
                var httpContextBase = new HttpContextWrapper(httpContext);
                var routeData = new RouteData();
                requestContext = new RequestContext(httpContextBase, routeData);
            } else requestContext = httpContext.Request.RequestContext;

            return new UrlHelper(requestContext);
        }

        static RegionInfo ResolveCountry()
        {
            var culture = ResolveCulture();
            if (culture != null) return new RegionInfo(culture.LCID);

            return null;
        }

        static CultureInfo ResolveCulture()
        {
            if (HttpContext.Current == null) return null;

            var languages = HttpContext.Current.Request.UserLanguages;

            if (languages == null || languages.Length == 0) return null;

            try {
                var language = languages[0].ToLowerInvariant().Trim();
                return CultureInfo.CreateSpecificCulture(language);
            } catch (ArgumentException) {
                return null;
            }
        }


        public static AjaxOptions GetAjaxOptions(string targetID, bool updateHistory = true)
        {
            var ret = new AjaxOptions { UpdateTargetId = targetID, OnComplete = string.Format("GlobalPageInit($('#{0}'))", targetID), };
            if (updateHistory) ret.OnSuccess = string.Format("ReplaceHistory($('#{0}').find('form').serialize())", targetID);
            return ret;
        }
        
        static void SetupPaypalInterface()
        {
            PayPalInterface = new PayPalInterface();
            PayPalInterface.Error +=
                (e) => {
                    LobbyApi.GhostSay(new Say() {
                        IsEmote = true,
                        Target = "zkdev",
                        User = GlobalConst.NightwatchName,
                        Text = "PAYMENT ERROR: " + e.ToString()
                    });
                };

            PayPalInterface.NewContribution += (c) => {
                var message = string.Format("WOOHOO! {0:d} New contribution of {1:F2}€ - for the jar {2}", c.Time, c.Euros,
                    c.ContributionJar.Name);

                LobbyApi.GhostSay(new Say() { IsEmote = true, Target = "zk", User = GlobalConst.NightwatchName, Text = message });

                if (c.AccountByAccountID != null) {
                    LobbyApi.GhostSay(new Say() {
                        IsEmote = true,
                        Target = "zk",
                        User = GlobalConst.NightwatchName,
                        Text = string.Format("It is {0} {2}/Users/Detail/{1}", c.AccountByAccountID.Name, c.AccountID, GlobalConst.BaseSiteUrl)
                    });
                }
            };
        }
    }
}
