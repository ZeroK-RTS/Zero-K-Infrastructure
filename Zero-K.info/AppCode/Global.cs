using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web;
using System.Web.Mvc;
using System.Globalization;
using System.Linq;
using CaTracker;
using LobbyClient;
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
            set { nightwatch = value; }
        }

        static PlanetWarsMatchMaker planetWarsMatchMaker;
        public static PlanetWarsMatchMaker PlanetWarsMatchMaker
        {
            get
            {
                if (planetWarsMatchMaker != null) return planetWarsMatchMaker;
                planetWarsMatchMaker = (PlanetWarsMatchMaker)HttpContext.Current.Application["PwMatchMaker"];
                return planetWarsMatchMaker;
            }
            set { planetWarsMatchMaker = value; }
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
        public static bool IsWebLobbyAccess { get { return HttpContext.Current.Session["weblobby"] != null; } }
        

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

            ev.PlainText = string.Format(format, args);
            var orgArgs = new List<object>(args);
            List<object> alreadyAddedEvents = new List<object>();

            for (var i = 0; i < args.Length; i++)
            {
                bool dontDuplicate = false; // set to true for args that have their own Event table in DB, e.g. accounts, factions, clans, planets
                var arg = args[i];
                var url = new UrlHelper(HttpContext.Current.Request.RequestContext);
                bool eventAlreadyExists = alreadyAddedEvents.Contains(arg);

                if (arg is Account)
                {
                    var acc = (Account)arg;
                    args[i] = HtmlHelperExtensions.PrintAccount(null, acc);
                    if (!eventAlreadyExists)
                    {
                        if (acc.AccountID != 0)
                        {
                            if (!ev.EventAccounts.Any(x => x.AccountID == acc.AccountID)) ev.EventAccounts.Add(new EventAccount() { AccountID = acc.AccountID });
                        }
                        else if (!ev.EventAccounts.Any(x => x.Account == acc)) ev.EventAccounts.Add(new EventAccount() { Account = acc });
                        dontDuplicate = true;
                    }
                }
                else if (arg is Clan)
                {
                    var clan = (Clan)arg;
                    args[i] = HtmlHelperExtensions.PrintClan(null, clan);
                    if (!eventAlreadyExists)
                    {
                        if (clan.ClanID != 0) ev.EventClans.Add(new EventClan() { ClanID = clan.ClanID });
                        else ev.EventClans.Add(new EventClan() { Clan = clan });
                        dontDuplicate = true;
                    }
                }
                else if (arg is Planet)
                {
                    var planet = (Planet)arg;
                    args[i] = HtmlHelperExtensions.PrintPlanet(null, planet);
                    if (!eventAlreadyExists)
                    {
                        if (planet.PlanetID != 0) ev.EventPlanets.Add(new EventPlanet() { PlanetID = planet.PlanetID });
                        else ev.EventPlanets.Add(new EventPlanet() { Planet = planet });
                        dontDuplicate = true;
                    }
                }
                else if (arg is SpringBattle)
                {
                    var bat = (SpringBattle)arg;
                    args[i] = string.Format("<a href='{0}'>B{1}</a>",
                                            url.Action("Detail", "Battles", new { id = bat.SpringBattleID }),
                                            bat.SpringBattleID); //todo no proper helper for this
                    if (!eventAlreadyExists)
                    {
                        if (bat.SpringBattleID != 0) ev.EventSpringBattles.Add(new EventSpringBattle() { SpringBattleID = bat.SpringBattleID });
                        else ev.EventSpringBattles.Add(new EventSpringBattle() { SpringBattle = bat });

                        foreach (Account acc in bat.SpringBattlePlayers.Where(sb => !sb.IsSpectator).Select(x => x.Account).Where(y => ev.EventAccounts.First(z => z.AccountID == y.AccountID) == null))
                        {
                            if (acc.AccountID != 0)
                            {
                                if (!ev.EventAccounts.Any(x => x.AccountID == acc.AccountID)) ev.EventAccounts.Add(new EventAccount() { AccountID = acc.AccountID });
                            }
                            else if (!ev.EventAccounts.Any(x => x.Account == acc)) ev.EventAccounts.Add(new EventAccount() { Account = acc });
                        }
                        dontDuplicate = true;
                    }
                }
                else if (arg is Faction)
                {
                    var fac = (Faction)arg;
                    args[i] = HtmlHelperExtensions.PrintFaction(null, fac, false);
                    if (!eventAlreadyExists)
                    {
                        if (fac.FactionID != 0) ev.EventFactions.Add(new EventFaction() { FactionID = fac.FactionID });
                        else ev.EventFactions.Add(new EventFaction() { Faction = fac });
                        dontDuplicate = true;
                    }
                }
                else if (arg is StructureType)
                {
                    var stype = (StructureType)arg;
                    args[i] = HtmlHelperExtensions.PrintStructureType(null, stype);
                }
                else if (arg is FactionTreaty) {
                    var tr = (FactionTreaty)arg;
                    args[i] = HtmlHelperExtensions.PrintFactionTreaty(null, tr);
                }
                else if (arg is RoleType) {
                    var rt = (RoleType)arg;
                    args[i] = HtmlHelperExtensions.PrintRoleType(null, rt);
                }

                if (dontDuplicate)
                {
                    alreadyAddedEvents.Add(arg);
                }
            }

            
            ev.Text = string.Format(format, args);
            try {
                var tas = nightwatch.Tas;
                if (tas != null) {
                    foreach (var clan in orgArgs.OfType<Clan>().Where(x => x != null)) {
                        tas.Say(TasClient.SayPlace.Channel, clan.GetClanChannel(), ev.PlainText, true);
                    }
                    foreach (var faction in orgArgs.OfType<Faction>().Where(x=>x!=null)) {
                        tas.Say(TasClient.SayPlace.Channel, faction.Shortcut, ev.PlainText, true);
                    }
                }
            } catch (Exception ex) {
                Trace.TraceError("Error sending event to channels: {0}",ex);
            }

            return ev;
        }

        public static CampaignEvent CreateCampaignEvent(int accountID, int campaignID, string format, params object[] args)
        {
            var ev = new CampaignEvent() { AccountID = accountID, CampaignID = campaignID, Time = DateTime.UtcNow };

            ev.PlainText = string.Format(format, args);
            var orgArgs = new List<object>(args);


            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                var url = new UrlHelper(HttpContext.Current.Request.RequestContext);

                if (arg is Account)
                {
                    /*
                    var acc = (Account)arg;
                    args[i] = HtmlHelperExtensions.PrintAccount(null, acc);
                    if (acc.AccountID != 0)
                    {
                        if (!ev.EventAccounts.Any(x => x.AccountID == acc.AccountID)) ev.EventAccounts.Add(new EventAccount() { AccountID = acc.AccountID });
                    }
                    else if (!ev.EventAccounts.Any(x => x.Account == acc)) ev.EventAccounts.Add(new EventAccount() { Account = acc });
                    */
                }
                else if (arg is CampaignPlanet)
                {
                    var planet = (CampaignPlanet)arg;
                    args[i] = HtmlHelperExtensions.PrintCampaignPlanet(null, planet);
                    ev.PlanetID = planet.PlanetID;
                }
            }


            ev.Text = string.Format(format, args);
            return ev;
        }
    }
}