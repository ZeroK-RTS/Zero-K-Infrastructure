using System;
using System.Web;
using System.Web.Mvc;
using System.Globalization;
using System.Linq;
using CaTracker;
using ZkData;

namespace ZeroKWeb
{
    public static class Global
    {
        static Nightwatch nightwatch;
        public static Nightwatch Nightwatch {
            get {
                if (nightwatch != null) return nightwatch;
                nightwatch = (Nightwatch)HttpContext.Current.Application["Nightwatch"];
                return nightwatch;
            }
        }


        public const int AjaxScrollCount = 40;
        public static Account Account { get { return HttpContext.Current.User as Account; } }
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
                if (IsAccountAuthorized && Account.Faction != null) return Account.FactionID ??0;
                else return 0;
            }
        }

        private static CultureInfo ResolveCulture()
        {
            if (HttpContext.Current == null)
                return null;

            string[] languages = HttpContext.Current.Request.UserLanguages;

            if (languages == null || languages.Length == 0)
                return null;

            try
            {
                string language = languages[0].ToLowerInvariant().Trim();
                return CultureInfo.CreateSpecificCulture(language);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }


        private static RegionInfo ResolveCountry()
        {
            CultureInfo culture = ResolveCulture();
            if (culture != null)
                return new RegionInfo(culture.LCID);

            return null;
        }

        private static string ResolveLanguage()
        {
            if (IsAccountAuthorized)
            {
                var db = new ZkDataContext();
                var acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);
                string manualLanguage = acc == null ? null : acc.Language;

                if (!String.IsNullOrEmpty(manualLanguage))
                    return manualLanguage;
            }

            RegionInfo ri = ResolveCountry();
            if (ri != null && !String.IsNullOrEmpty(ri.TwoLetterISORegionName))
                return ri.TwoLetterISORegionName;

            return "en";
        }

        public static bool IsAccountAuthorized { get { return HttpContext.Current.User as Account != null; } }

        public static bool IsLobbyAccess { get { return HttpContext.Current.Request.Cookies[GlobalConst.LobbyAccessCookieName] != null; } }
        public static bool IsLobbyAdmin { get { return IsAccountAuthorized && Account.IsLobbyAdministrator; } }
        public static bool IsZeroKAdmin { get { return IsAccountAuthorized && Account.IsZeroKAdmin; } }

        public static string DisplayLanguage { get { return ResolveLanguage(); } }
        public static UserLanguage DisplayLanguageAsEnum { get {
            try
            {
                return (UserLanguage)Enum.Parse(typeof(UserLanguage), DisplayLanguage, true); 
            }
            catch (System.Exception ex)
            {
                return UserLanguage.auto;
            }            
        } }

        public static Event CreateEvent(string format, params object[] args)
        {
            var ev = new Event() { Time = DateTime.UtcNow };

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                var url = new UrlHelper(HttpContext.Current.Request.RequestContext);

                if (arg is Account)
                {
                    var acc = (Account)arg;
                    args[i] = HtmlHelperExtensions.PrintAccount(null, acc, false);
                    if (acc.AccountID != 0) {
                        if (!ev.EventAccounts.Any(x=>x.AccountID == acc.AccountID)) ev.EventAccounts.Add(new EventAccount() { AccountID = acc.AccountID });
                    }
                    else if (!ev.EventAccounts.Any(x=>x.Account == acc)) ev.EventAccounts.Add(new EventAccount() { Account = acc });
                }
                else if (arg is Clan)
                {
                    var clan = (Clan)arg;
                    args[i] = HtmlHelperExtensions.PrintClan(null, clan, false);
                    if (clan.ClanID != 0) ev.EventClans.Add(new EventClan() { ClanID = clan.ClanID });
                    else ev.EventClans.Add(new EventClan() { Clan = clan });
                }
                else if (arg is Planet)
                {
                    var planet = (Planet)arg;
                    args[i] = HtmlHelperExtensions.PrintPlanet(null, planet);
                    if (planet.PlanetID != 0) ev.EventPlanets.Add(new EventPlanet() { PlanetID = planet.PlanetID });
                    else ev.EventPlanets.Add(new EventPlanet() { Planet = planet });
                }
                else if (arg is SpringBattle)
                {
                    var bat = (SpringBattle)arg;
                    args[i] = string.Format("<a href='{0}'>B{1}</a>",
                                            url.Action("Detail", "Battles", new { id = bat.SpringBattleID }),
                                            bat.SpringBattleID); //todo no propoer helper for this
                    if (bat.SpringBattleID != 0) ev.EventSpringBattles.Add(new EventSpringBattle() { SpringBattleID = bat.SpringBattleID });
                    else ev.EventSpringBattles.Add(new EventSpringBattle() { SpringBattle = bat });
                }
                else if (arg is Faction)
                {
                    var fac = (Faction)arg;
                    args[i] = HtmlHelperExtensions.PrintFaction(null, fac, false);
                    
                }
                else if (arg is RoleType) {
                    var rt = (RoleType)arg;
                    args[i] = HtmlHelperExtensions.PrintRoleType(null, rt);
                }

            }


            ev.Text = string.Format(format, args);
            return ev;
        }
    }
}