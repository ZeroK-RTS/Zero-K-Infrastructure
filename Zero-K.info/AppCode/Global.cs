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
        public static ZkLobbyServer.ZkLobbyServer Server { get; private set; }

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

            GlobalConst.SiteDiskPath = MapPath("~");
            
            AwardCalculator = new AwardCalculator();
            AwardCalculator.RecomputeNow();

            var sitePath = mvcApplication.Server.MapPath("~");
            ZkServerRunner = new ServerRunner(sitePath, new PlanetwarsEventCreator());
            Server = ZkServerRunner.ZkLobbyServer;

            Trace.TraceInformation("Starting lobby server");
            ZkServerRunner.Run();
            listener.ZkLobbyServer = Server;

            ForumPostIndexer = new ForumPostIndexer();

            SteamDepotGenerator = new SteamDepotGenerator(sitePath, Path.Combine(sitePath, "..", "steamworks", "tools", "ContentBuilder", "content"));

            
            SetupPaypalInterface();


            // HACK Task.Factory.StartNew(() => SteamDepotGenerator.RunAll());

            Trace.TraceInformation("Starting autoregistrator");
            AutoRegistrator = new AutoRegistrator(MapPath("~"));
            AutoRegistrator.NewZkReleaseRegistered += (game, chobby) =>
            {
                SteamDepotGenerator.RunAll();
                Server.SetGame(game);
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
                    Server.GhostSay(new Say() {
                        IsEmote = true,
                        Target = "zkdev",
                        User = GlobalConst.NightwatchName,
                        Text = "PAYMENT ERROR: " + e.ToString()
                    });
                };

            PayPalInterface.NewContribution += (c) => {
                var message = string.Format("WOOHOO! {0:d} New contribution of {1:F2}€ - for the jar {2}", c.Time, c.Euros,
                    c.ContributionJar.Name);

                Server.GhostSay(new Say() { IsEmote = true, Target = "zk", User = GlobalConst.NightwatchName, Text = message });

                if (c.AccountByAccountID != null) {
                    Server.GhostSay(new Say() {
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
