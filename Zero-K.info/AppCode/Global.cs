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
using System.Web.Routing;
using JetBrains.Annotations;
using LobbyClient;
using ZkData;
using ZkLobbyServer;

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
        public static string DisplayLanguage
        {
            get { return ResolveLanguage(); }
        }
        public static UserLanguage DisplayLanguageAsEnum
        {
            get
            {
                try {
                    return (UserLanguage)Enum.Parse(typeof(UserLanguage), DisplayLanguage, true);
                } catch (Exception ex) {
                    return UserLanguage.auto;
                }
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
        public static bool IsZeroKAdmin
        {
            get { return IsAccountAuthorized && Account.IsZeroKAdmin; }
        }
        public static PayPalInterface PayPalInterface { get; private set; }
        public static PlanetWarsMatchMaker PlanetWarsMatchMaker { get; private set; }
        public static ZkLobbyServer.ZkLobbyServer Server { get; private set; }

        public static ServerRunner ZkServerRunner { get; private set; }


        /// <summary>
        /// Converts a given string and its arguments into a CampaignEvent; used for the online SP campaign
        /// </summary>
        /// <param name="accountID">Player's account ID</param>
        /// <param name="campaignID">Campaign ID</param>
        /// <param name="format">String to format; converted objects are inserted into the string - e.g. "Journal unlocked: {0}"</param>
        /// <param name="args">Objects can be <see cref="Account"/> or <see cref="CampaignPlanet"/></param>
        public static CampaignEvent CreateCampaignEvent(int accountID, int campaignID, string format, params object[] args)
        {
            var ev = new CampaignEvent() { AccountID = accountID, CampaignID = campaignID, Time = DateTime.UtcNow };

            ev.PlainText = string.Format(format, args);
            var orgArgs = new List<object>(args);

            for (var i = 0; i < args.Length; i++) {
                var arg = args[i];
                var url = UrlHelper();

                if (arg is Account) {
                    /*
                    var acc = (Account)arg;
                    args[i] = HtmlHelperExtensions.PrintAccount(null, acc);
                    if (acc.AccountID != 0)
                    {
                        if (!ev.EventAccounts.Any(x => x.AccountID == acc.AccountID)) ev.EventAccounts.Add(new EventAccount() { AccountID = acc.AccountID });
                    }
                    else if (!ev.EventAccounts.Any(x => x.Account == acc)) ev.EventAccounts.Add(new EventAccount() { Account = acc });
                    */
                } else if (arg is CampaignPlanet) {
                    var planet = (CampaignPlanet)arg;
                    args[i] = HtmlHelperExtensions.PrintCampaignPlanet(null, planet);
                    ev.PlanetID = planet.PlanetID;
                }
            }

            ev.Text = string.Format(format, args);
            return ev;
        }

        /// <summary>
        /// Converts a given string and its arguments into an Event
        /// </summary>
        /// <param name="format">String to format; converted objects are inserted into the string - e.g. "Planet {0} captured by {1}"</param>
        /// <param name="args">Objects can be DB objects of various types; e.g. <see cref="Account"/>, <see cref="Clan"/>, <see cref="Planet"/></param>
        [StringFormatMethod("format")]
        public static Event CreateEvent(string format, params object[] args)
        {
            var ev = new Event() { Time = DateTime.UtcNow };

            ev.PlainText = string.Format(format, args);
            var orgArgs = new List<object>(args);
            var alreadyAddedEvents = new List<object>();

            for (var i = 0; i < args.Length; i++) {
                var dontDuplicate = false; // set to true for args that have their own Event table in DB, e.g. accounts, factions, clans, planets
                var arg = args[i];
                if (arg == null) continue;
                var url = UrlHelper();
                var eventAlreadyExists = alreadyAddedEvents.Contains(arg);

                if (arg is Account) {
                    var acc = (Account)arg;
                    args[i] = HtmlHelperExtensions.PrintAccount(null, acc);
                    if (!eventAlreadyExists) {
                        if (!ev.Accounts.Any(x => x.AccountID == acc.AccountID)) ev.Accounts.Add(acc);
                        dontDuplicate = true;
                    }
                } else if (arg is Clan) {
                    var clan = (Clan)arg;
                    args[i] = HtmlHelperExtensions.PrintClan(null, clan);
                    if (!eventAlreadyExists) {
                        ev.Clans.Add(clan);
                        dontDuplicate = true;
                    }
                } else if (arg is Planet) {
                    var planet = (Planet)arg;
                    args[i] = HtmlHelperExtensions.PrintPlanet(null, planet);
                    if (!eventAlreadyExists) {
                        if (planet.PlanetID != 0) ev.Planets.Add(planet);
                        dontDuplicate = true;
                    }
                } else if (arg is SpringBattle) {
                    var bat = (SpringBattle)arg;
                    args[i] = string.Format("<a href='{0}'>B{1}</a>", url.Action("Detail", "Battles", new { id = bat.SpringBattleID }),
                        bat.SpringBattleID); //todo no proper helper for this
                    if (!eventAlreadyExists) {
                        ev.SpringBattles.Add(bat);

                        foreach (
                            var acc in
                                bat.SpringBattlePlayers.Where(sb => !sb.IsSpectator)
                                    .Select(x => x.Account)
                                    .Where(y => !ev.Accounts.Any(z => z.AccountID == y.AccountID))) {
                            if (acc.AccountID != 0) {
                                if (!ev.Accounts.Any(x => x.AccountID == acc.AccountID)) ev.Accounts.Add(acc);
                            } else if (!ev.Accounts.Any(x => x == acc)) ev.Accounts.Add(acc);
                        }
                        dontDuplicate = true;
                    }
                } else if (arg is Faction) {
                    var fac = (Faction)arg;
                    args[i] = HtmlHelperExtensions.PrintFaction(null, fac, false);
                    if (!eventAlreadyExists) {
                        ev.Factions.Add(fac);
                        dontDuplicate = true;
                    }
                } else if (arg is StructureType) {
                    var stype = (StructureType)arg;
                    args[i] = HtmlHelperExtensions.PrintStructureType(null, stype);
                } else if (arg is FactionTreaty) {
                    var tr = (FactionTreaty)arg;
                    args[i] = HtmlHelperExtensions.PrintFactionTreaty(null, tr);
                } else if (arg is RoleType) {
                    var rt = (RoleType)arg;
                    args[i] = HtmlHelperExtensions.PrintRoleType(null, rt);
                }

                if (dontDuplicate) alreadyAddedEvents.Add(arg);
            }

            ev.Text = string.Format(format, args);
            try {
                if (Server != null) {
                    foreach (var clan in orgArgs.OfType<Clan>().Where(x => x != null)) Server.GhostSay(
                        new Say() { User = GlobalConst.NightwatchName, IsEmote = true, Place = SayPlace.Channel,Target = clan.GetClanChannel(), Text = ev.PlainText});
                    foreach (var faction in orgArgs.OfType<Faction>().Where(x => x != null)) Server.GhostSay(
                         new Say() { User = GlobalConst.NightwatchName, IsEmote = true, Place = SayPlace.Channel, Target = faction.Shortcut, Text = ev.PlainText });
                }
            } catch (Exception ex) {
                Trace.TraceError("Error sending event to channels: {0}", ex);
            }

            return ev;
        }

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

        public static void StartApplication(MvcApplication mvcApplication)
        {
            var listener = new ZkServerTraceListener();
            Trace.Listeners.Add(listener);

            ZkServerRunner = new ServerRunner(mvcApplication.Server.MapPath("~"));
            Server = ZkServerRunner.ZkLobbyServer;
            ZkServerRunner.Run();
            listener.ZkLobbyServer = Server;

            SetupPaypalInterface();

            if (GlobalConst.PlanetWarsMode == PlanetWarsModes.Running) PlanetWarsMatchMaker = new PlanetWarsMatchMaker(Server);
        }

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

        static string ResolveLanguage()
        {
            if (IsAccountAuthorized) {
                var db = new ZkDataContext();
                var acc = db.Accounts.Single(x => x.AccountID == AccountID);
                var manualLanguage = acc == null ? null : acc.Language;

                if (!String.IsNullOrEmpty(manualLanguage)) return manualLanguage;
            }

            var ri = ResolveCountry();
            if (ri != null && !String.IsNullOrEmpty(ri.TwoLetterISORegionName)) return ri.TwoLetterISORegionName;

            return "en";
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
                var message = string.Format("WOOHOO! {0:d} New contribution of {1:F2}€ by {2} - for the jar {3}", c.Time, c.Euros,
                    c.Name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(), c.ContributionJar.Name);

                Server.GhostSay(new Say() { IsEmote = true, Target = "zkdev", User = GlobalConst.NightwatchName, Text = message });

                if (c.AccountByAccountID != null) {
                    Server.GhostSay(new Say() {
                        IsEmote = true,
                        Target = "zkdev",
                        User = GlobalConst.NightwatchName,
                        Text = string.Format("It is {0} {2}/Users/Detail/{1}", c.AccountByAccountID.Name, c.AccountID, GlobalConst.BaseSiteUrl)
                    });
                }
            };
        }
    }
}