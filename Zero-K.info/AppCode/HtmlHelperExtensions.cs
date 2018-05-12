using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Mvc.Html;
using System.Web.WebPages;
using ZeroKWeb;
using ZeroKWeb.ForumParser;
using ZkData;

namespace System.Web.Mvc
{
    public enum StarType
    {
        RedStarSmall,
        GreenStarSmall,
        WhiteStarSmall,
        RedSkull,
        WhiteSkull
    }

    public class SelectOption
    {
        public string Name;
        public string Value;
    }

    /// <summary>
    /// <para>Contains functions that return a <see cref="MvcHtmlString"/> for pretty display of things like accounts and clans</para>
    /// <para>The returned string is often also a link leading to the appropriate page, and has its own tooltip</para>
    /// </summary>
    public static class HtmlHelperExtensions
    {
        public static MvcHtmlString AccountAvatar(this HtmlHelper helper, Account account) {
            if (account.IsDeleted) return null;
            return new MvcHtmlString(string.Format("<img src='/img/avatars/{0}.png' class='avatar'>", account.Avatar));
        }

        /// <summary>
        /// Uses regexes to turn a string into BBcode
        /// </summary>
        public static MvcHtmlString BBCode(this HtmlHelper helper, string str) {
            if (str == null) return null;
            return new MvcHtmlString(new ForumWikiParser().TranslateToHtml(str, helper));
        }

        public static MvcHtmlString BBCodeCached(this HtmlHelper helper, ForumPost post) {
            return Global.ForumPostCache.GetCachedHtml(post, helper);
        }

        public static MvcHtmlString BBCodeCached(this HtmlHelper helper, News news)
        {
            return Global.ForumPostCache.GetCachedHtml(news, helper);
        }


        /// <summary>
        /// Used for boolean dropdown selections on the site; e.g. map search filter
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="name">Value tag; e.g. "is1v1" or "chickens" for maps</param>
        /// <param name="selected"></param>
        /// <param name="anyItem">If true, allows an "any" option (indicated by an ?)</param>
        /// <returns></returns>
        public static MvcHtmlString BoolSelect(this HtmlHelper helper, string name, bool? selected, string anyItem) {
            var sb = new StringBuilder();
            sb.AppendFormat("<select name='{0}'>", helper.Encode(name));
            if (anyItem != null) sb.AppendFormat("<option {1}>{0}</option>", helper.Encode(anyItem), selected == null ? "selected" : "");
            sb.AppendFormat("<option value='True' {0}>Yes</option>", selected == true ? "selected" : "");
            sb.AppendFormat("<option value='False' {0}>No</option>", selected == false ? "selected" : "");

            sb.Append("</select>");
            return new MvcHtmlString(sb.ToString());
        }

        public static IEnumerable<SelectListItem> GetFactionItems(this HtmlHelper html, int factionID, Expression<Func<Faction, bool>> filter = null) {
            var ret = new ZkDataContext().Factions.AsQueryable().Where(x => !x.IsDeleted);
            if (filter != null) ret = ret.Where(filter);
            return ret.Select(x => new SelectListItem { Text = x.Name, Value = x.FactionID.ToString(), Selected = x.FactionID == factionID });
        }

        public static MvcHtmlString IncludeFile(this HtmlHelper helper, string name) {
            if (name.StartsWith("http://")) {
                var ret = new WebClient().DownloadString(name);
                return new MvcHtmlString(ret);
            }
            else {
                var path = Global.MapPath(name);
                return new MvcHtmlString(File.ReadAllText(path));
            }
        }


        public static MvcHtmlString IncludeWiki(this HtmlHelper helper, string node) {
            var post = new ZkDataContext().ForumThreads.FirstOrDefault(x => x.WikiKey == node)?.ForumPosts.OrderBy(x => x.ForumPostID).FirstOrDefault();
            if (post == null) return null;
            return Global.ForumPostCache.GetCachedHtml(post, helper);
        }

        /// <summary>
        ///     <para>Returns an appropriately formatted link with thread title and mail icon for a thread</para>
        ///     <para>(e.g. bold = posted in; italics = new; grey icon = already read)</para>
        /// </summary>
        /// <param name="thread">The thread to print</param>
        /// <returns></returns>
        public static MvcHtmlString Print(this HtmlHelper helper, ForumThread thread) {
            var url = Global.UrlHelper();

            ForumThreadLastRead lastRead = null;
            ForumLastRead lastReadForum = null;
            DateTime? lastTime = null;
            if (Global.Account != null)
            {
                lastRead = Global.Account.ForumThreadLastReads.FirstOrDefault(x => x.ForumThreadID == thread.ForumThreadID);
                lastReadForum = Global.Account.ForumLastReads.FirstOrDefault(x => x.ForumCategoryID == thread.ForumCategoryID);
                if (lastReadForum != null) lastTime = lastReadForum.LastRead;
            }
            if (lastRead != null && (lastTime == null || lastRead.LastRead > lastTime)) lastTime = lastRead.LastRead;
            ForumPost post = null;
            if (lastTime != null) post = thread.ForumPosts.FirstOrDefault(x => x.Created > lastTime);
            int page = post != null ? ZeroKWeb.Controllers.ForumController.GetPostPage(post) : (thread.PostCount-1)/GlobalConst.ForumPostsPerPage;

            string link;
            if (page > 0) link = url.Action("Thread", "Forum", new { id = thread.ForumThreadID, page = page});
            else link = url.Action("Thread", "Forum", new { id = thread.ForumThreadID });
            link = string.Format("<a href='{0}' title='$thread${1}' style='word-break:break-all;'>", link, thread.ForumThreadID);

            string format;

            if (lastTime == null) format = "<span>{0}<img src='/img/mail/mail-unread.png' height='15' /><i>{1}</i></a></span>";
            else {
                if (lastTime >= thread.LastPost) format = "<span>{0}<img src='/img/mail/mail-read.png' height='15' />{1}</a></span>";
                else {
                    if (lastRead != null && lastRead.LastPosted != null) format = "<span>{0}<img src='/img/mail/mail-new.png' height='15' /><b>{1}</b></a></span>";
                    else format = "<span>{0}<img src='/img/mail/mail-unread.png' height='15' />{1}</a></span>";
                }
            }

            string title = HttpUtility.HtmlEncode(thread.Title);
            if (!string.IsNullOrEmpty(thread.WikiKey))
            {
                title = string.Format("<span style='color:lightblue'>[{0}]</span> {1}", thread.WikiKey, title);
            }

            return new MvcHtmlString(string.Format(format, link, title));
        }

        /// <summary>
        /// Returns an appropriately formatted link with username and relevant user icons leading to an account page
        /// </summary>
        /// <param name="account">Account to print</param>
        /// <param name="colorize">If true, write the user name in <see cref="Faction"/> color</param>
        /// <param name="ignoreDeleted">If false, just prints "{redacted}" for accounts marked as deleted</param>
        public static MvcHtmlString PrintAccount(this HtmlHelper helper, Account account, bool colorize = true, bool ignoreDeleted = false) {
            if (account == null) return new MvcHtmlString("Nobody");
            else if (account.IsDeleted && !ignoreDeleted) return new MvcHtmlString("{redacted}");
            else {
                var clanStr = "";
                var url = Global.UrlHelper();
                if (account.Clan != null) {
                    clanStr = string.Format("<a href='{1}' nicetitle='$clan${2}'><img src='{0}' width='16'/></a>",
                                            account.Clan.GetImageUrl(),
                                            url.Action("Detail", "Clans", new { id = account.ClanID }),
                                            account.ClanID);
                }
                else if (account.Faction != null) clanStr = string.Format("<img src='{0}' width='16'/>", account.Faction.GetImageUrl());
                
                var dudeStr = "";
                if (account.AdminLevel >= AdminLevel.Moderator) dudeStr = "<img src='/img/police.png'  class='icon16' alt='Admin' />";
                
                string color = Faction.FactionColor(account.Faction, Global.FactionID);
                if (String.IsNullOrEmpty(color)) color = "#B0D0C0";

                return
                    new MvcHtmlString(
                        string.Format(
                            "<img src='/img/flags/{0}.png' class='flag' height='11' width='16' alt='{0}'/><img src='/img/ranks/{1}.png'  class='icon16' alt='rank' />{5}{6}<a href='/Users/Detail/{2}' style='color:{3}' nicetitle='$user${2}'>{4}</a>",
                            (account.Country != "??" && !account.HideCountry) ? account.Country : "unknown",
                            account.GetIconName(),
                            account.AccountID,
                            colorize ? color : "",
                            account.Name,
                            clanStr,
                            dudeStr));
            }
        }

        public static MvcHtmlString PrintDate(this HtmlHelper helper, DateTime? dateTime) {
            return new MvcHtmlString($"<span nicetitle=\"{dateTime}\">{dateTime.ToAgoString()}</span>");    
        }

        public static MvcHtmlString PrintSeconds(this HtmlHelper helper, int? seconds)
        {
            if (seconds != null) return new MvcHtmlString($"<span nicetitle=\"{seconds}\">{TimeSpan.FromSeconds(seconds.Value).ToNiceString()}</span>");
            else return new MvcHtmlString("");
        }

        /// <summary>
        /// <para>Returns an appropriately formatted link with battle ID, player count, map and icons leading to the battle page</para>
        /// <para>e.g. [Multiplayer icon] B360800 10 on Coagulation Marsh 0.6</para>
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="battlePlayer">If specified player is in the battle, draw a win/lose icon as appropriate; else draw the spectator icon</param>
        /// <returns></returns>
        public static MvcHtmlString PrintBattle(this HtmlHelper helper, SpringBattlePlayer battlePlayer) {
            if (battlePlayer == null) return null;
            return PrintBattle(helper, battlePlayer.SpringBattle, battlePlayer.IsSpectator ? null : (bool?)battlePlayer.IsInVictoryTeam);
        }

        public static MvcHtmlString PrintBattle(this HtmlHelper helper, SpringBattle battle, bool? isVictory = null) {
            var url = Global.UrlHelper();
            var icon = "";
            if (isVictory == true) icon = "battlewon.png";
            else if (isVictory == null) icon = "spec.png";
            else icon = "battlelost.png";

            icon = string.Format("<img src='/img/battles/{0}' class='vcenter' />", icon);

            if (battle.IsMission) icon += " <img src='/img/battles/mission.png' alt='Mission' class='vcenter' />";
            if (battle.HasBots) icon += " <img src='/img/battles/robot.png' alt='Bots' class='vcenter' />";

            if (battle.BattleType == "Multiplayer") icon += " <img src='/img/battles/multiplayer.png' alt='Multiplayer' class='vcenter' />";
            else if (battle.BattleType == "Singleplayer") icon += " <img src='/img/battles/singleplayer.png' alt='Singleplayer' class='vcenter' />";

            return
                new MvcHtmlString(string.Format("<span><a href='{0}'>{4} B{1}</a> {2} on {3}</span>",
                                                url.Action("Detail", "Battles", new { id = battle.SpringBattleID }),
                                                battle.SpringBattleID,
                                                battle.PlayerCount,
                                                PrintMap(helper, battle.ResourceByMapResourceID?.InternalName),
                                                icon));
        }

        /// <summary>
        /// Returns the specified number followed by the PlanetWars bomber icon
        /// </summary>
        public static MvcHtmlString PrintBombers(this HtmlHelper helper, double? count) {
            return new MvcHtmlString(string.Format("<span>{0}<img src='{1}' class='icon20'/></span>", count ?? 0, GlobalConst.BomberIcon));
        }

        /// <summary>
        /// <para>Returns the PlanetWars bomber icon, the number of bombers available to the specified account, and the number of bombers the faction as a whole has</para>
        /// <para>e.g. [bomber icon]0 / 31</para>
        /// </summary>
        public static MvcHtmlString PrintBombers(this HtmlHelper helper, Account account) {
            if (account != null && account.Faction != null) {
                var ownShips = account.GetBombersAvailable();
                var factionShips = account.Faction.Bombers;
                return
                    new MvcHtmlString(
                        string.Format("<span nicetitle='Bombers available to you/owned by faction'><img src='{0}' class='icon20'/>{1} / {2}</span>",
                                      GlobalConst.BomberIcon,
                                      Math.Floor(ownShips),
                                      Math.Floor(factionShips)));
            }
            else return null;
        }

        /// <summary>
        /// Returns an appropriately formatted link with clan name and icon leading to the clan page
        /// </summary>
        /// <param name="colorize">If true, write the text in <see cref="Faction"/> color</param>
        /// <returns></returns>
        public static MvcHtmlString PrintClan(this HtmlHelper helper, Clan clan, bool colorize = true, bool big = false) {
            var url = Global.UrlHelper();
            if (clan == null) return new MvcHtmlString(string.Format("<a href='{0}'>No Clan</a>", url.Action("Index", "Clans")));
            {
                string color = Clan.ClanColor(clan, Global.ClanID);
                if (String.IsNullOrEmpty(color)) color = "#B0D0C0";
                if (big)
                {
                    return
                        new MvcHtmlString(string.Format("<a href='{1}' nicetitle='$clan${2}'><img width='64' src='{0}'/></a>",
                                                        clan.GetImageUrl(),
                                                        url.Action("Detail", "Clans", new { id = clan.ClanID }),
                                                        clan.ClanID));
                }
                else
                {
                    return new MvcHtmlString(
                        string.Format("<a href='{0}' nicetitle='$clan${4}'><img src='{1}' width='16'><span style='color:{2}'>{3}</span></a>",
                                      url.Action("Detail", "Clans", new { id = clan.ClanID }),
                                      clan.GetImageUrl(),
                                      colorize ? color : "",
                                      HttpUtility.HtmlEncode(clan.Shortcut),
                                      clan.ClanID));
                }
            }
        }


        public static MvcHtmlString PrintBadges(this HtmlHelper helper, Account account, int? maxWidth = null, bool newlines = true)
        {
            if (account == null || account.IsDeleted) return new MvcHtmlString("");
            var badges = account.GetBadges();
            return new MvcHtmlString(string.Join("\n", badges.Select(x=>$"<img src='/img/badges/{x}.png' nicetitle='{x.Description()}' {(maxWidth != null ? $"style='width:{maxWidth}px;'":"")}/>{(newlines ? "<br/>" : "")}")));
        }

        /// <summary>
        /// Returns the specified number followed by the PlanetWars dropship icon
        /// </summary>
        public static MvcHtmlString PrintDropships(this HtmlHelper helper, double? count, Faction faction) {
            return
                new MvcHtmlString(string.Format("<span>{0}<img src='{1}' class='icon20'/></span>", Math.Floor(count ?? 0), faction.GetShipImageUrl()));
        }

        /// <summary>
        /// <para>Returns the PlanetWars dropship icon, the number of dropships available to the specified account, and the number of dropships the faction as a whole has</para>
        /// <para>e.g. [dropship icon]0 / 11 </para>
        /// </summary>
        public static MvcHtmlString PrintDropships(this HtmlHelper helper, Account account) {
            if (account != null && account.Faction != null) {
                var ownShips = account.GetDropshipsAvailable();
                var factionShips = account.Faction.Dropships;
                return
                    new MvcHtmlString(
                        string.Format(
                            "<span nicetitle='Dropships available to you/owned by faction'><img src='{0}' class='icon20'/>{1} / {2}</span>",
                            account.Faction.GetShipImageUrl(),
                            Math.Floor(ownShips),
                            Math.Floor(factionShips)));
            }
            else return null;
        }

        /// <summary>
        /// Returns the specified number followed by the PlanetWars energy icon
        /// </summary>
        public static MvcHtmlString PrintEnergy(this HtmlHelper helper, double? count) {
            return new MvcHtmlString(string.Format("<span>{0}<img src='{1}' class='icon20'/></span>", Math.Floor(count ?? 0), GlobalConst.EnergyIcon));
        }

        /// <summary>
        /// Returns an <see cref="MvcHtmlString"/> representing the specified faction, with link
        /// </summary>
        /// <param name="fac">The faction to print</param>
        /// <param name="big">If true this just makes a big image of the faction icon; else it has a small faction icon followed by the faction short name</param>
        /// <returns></returns>
        public static MvcHtmlString PrintFaction(this HtmlHelper helper, Faction fac, bool big = true) {
            var url = Global.UrlHelper();
            if (fac != null) {
                if (big) {
                    return
                        new MvcHtmlString(string.Format("<a href='{1}' nicetitle='$faction${2}'><img src='{0}'/></a>",
                                                        fac.GetImageUrl(),
                                                        url.Action("Detail", "Factions", new { id = fac.FactionID }),
                                                        fac.FactionID));
                }
                else {
                    return
                        new MvcHtmlString(
                            string.Format(
                                "<a href='{3}' nicetitle='$faction${4}'><span style='color:{0}'><img src='{1}'  style='width:16px;height:16px'/>{2}</span></a>",
                                fac.Color,
                                fac.GetImageUrl(),
                                fac.Shortcut,
                                url.Action("Detail", "Factions", new { id = fac.FactionID }),
                                fac.FactionID));
                }
            }
            else return new MvcHtmlString("");
        }

        /// <summary>
        /// Returns a PlanetWars treaty ID with link
        /// </summary>
        public static MvcHtmlString PrintFactionTreaty(this HtmlHelper helper, FactionTreaty treaty) {
            var url = Global.UrlHelper();
            if (treaty != null) {
                return
                    new MvcHtmlString(string.Format("<a href='{1}' nicetitle='$treaty${0}'>TR{0}</span></a>",
                                                    treaty.FactionTreatyID,
                                                    url.Action("TreatyDetail", "Factions", new { id = treaty.FactionTreatyID })));
            }
            else return new MvcHtmlString("");
        }


        public static MvcHtmlString PrintInfluence(this HtmlHelper helper, PlanetFaction planetFaction) {
            return PrintInfluence(helper, planetFaction.Faction, planetFaction.Influence);
        }

        /// <summary>
        /// Returns a string of the % influence the specified <see cref="Faction"/> has on the <see cref="Planet"/>
        /// </summary>
        /// <param name="fac">The faction whose influence should be printed</param>
        /// <returns></returns>
        public static MvcHtmlString PrintInfluence(this HtmlHelper helper, Faction fac, double influence) {
            var formattedString = string.Format("<span style='color:{0}'>{1:0.#} ({2:0.#}%)</span>", Faction.FactionColor(fac, Global.FactionID), influence, 100 * influence / GlobalConst.PlanetWarsMaximumIP);
            return new MvcHtmlString(formattedString);
        }

        public static MvcHtmlString PrintInfluence(this HtmlHelper helper, Faction faction, int influence, int shadowInfluence) {
            var formatString = "<span style='color:{0}'>{1}</span>";
            if (shadowInfluence > 0) formatString += "&nbsp({2}&nbsp+&nbsp<span style='color:gray'>{3}</span>)";
            var formattedString = string.Format(formatString, faction.Color, influence + shadowInfluence, influence, shadowInfluence);
            return new MvcHtmlString(formattedString);
        }

        public static MvcHtmlString PrintLines(this HtmlHelper helper, string text) {
            return new MvcHtmlString(helper.Encode(text).Replace("\n", "<br/>"));
        }

        public static MvcHtmlString PrintLines(this HtmlHelper helper, IEnumerable<object> lines) {
            var sb = new StringBuilder();
            foreach (var line in lines) sb.AppendFormat("{0}<br/>", line);
            return new MvcHtmlString(sb.ToString());
        }

        public static MvcHtmlString PrintMap(this HtmlHelper helper, string name) {
            var url = Global.UrlHelper();
            return new MvcHtmlString(string.Format("<a href='{0}' title='$map${1}'>{1}</a>", url.Action("DetailName", "Maps", new { name }), name));
        }

        /// <summary>
        /// Returns the PlanetWars metal icon, the amount of metal available to the specified account, and the amount of metal the faction as a whole has
        /// </summary>
        public static MvcHtmlString PrintMetal(this HtmlHelper helper, Account account) {
            if (account != null && account.Faction != null) {
                var ownMetal = account.GetMetalAvailable();
                var factionMetal = Math.Floor(account.Faction.Metal);
                return
                    new MvcHtmlString(
                        string.Format(
                            "<span style='color:#00FFFF' nicetitle='Metal available to you/owned by faction'><img src='{0}' width='20' height='20'/>{1} / {2}</span>",
                            GlobalConst.MetalIcon,
                            Math.Floor(ownMetal),
                            Math.Floor(factionMetal)));
            }
            else return null;
        }

        /// <summary>
        /// Returns the specified number followed by the PlanetWars metal icon
        /// </summary>
        public static MvcHtmlString PrintMetal(this HtmlHelper helper, double? cost) {
            return
                new MvcHtmlString(string.Format("<span style='color:#00FFFF;'>{0}<img src='{1}' class='icon20'/></span>",
                                                Math.Floor(cost ?? 0),
                                                GlobalConst.MetalIcon));
        }

        /// <summary>
        /// Returns the PlanetWars <see cref="Planet"/> icon and name, colored in the owning faction color
        /// </summary>
        public static MvcHtmlString PrintPlanet(this HtmlHelper helper, Planet planet) {
            if (planet == null) return new MvcHtmlString("?");
            var url = Global.UrlHelper();
            return
                new MvcHtmlString(string.Format("<a href='{0}' title='$planet${4}' style='{5}'><img src='/img/planets/{1}' width='{2}'>{3}</a>",
                                                url.Action("Planet", "Planetwars", new { id = planet.PlanetID }),
                                                planet.Resource.MapPlanetWarsIcon,
                                                planet.Resource.PlanetWarsIconSize/3,
                                                planet.Name,
                                                planet.PlanetID,
                                                planet.Faction != null ? "color:" + planet.Faction.Color : ""));
        }


        /// <summary>
        /// Returns the clan/faction role name and tooltip
        /// </summary>
        /// <param name="rt">The <see cref="RoleType"/> to print</param>
        public static MvcHtmlString PrintRoleType(this HtmlHelper helper, RoleType rt) {
            var factoids = new List<string>();
            if (rt.IsClanOnly) factoids.Add("clan based");
            if (rt.IsOnePersonOnly) factoids.Add("only one person can hold this");

            if (rt.IsVoteable) factoids.Add("is voteable");
            if (rt.RoleTypeHierarchiesByMasterRoleTypeID.Any(x => x.CanAppoint)) {
                factoids.Add("appoints: " +
                             string.Join(", ",
                                         rt.RoleTypeHierarchiesByMasterRoleTypeID.Where(x => x.CanAppoint)
                                           .Select(x => x.SlaveRoleType.Name)));
            }
            if (rt.RoleTypeHierarchiesByMasterRoleTypeID.Any(x => x.CanRecall)) {
                factoids.Add("recalls: " +
                             string.Join(", ",
                                         rt.RoleTypeHierarchiesByMasterRoleTypeID.Where(x => x.CanRecall)
                                           .Select(x => x.SlaveRoleType.Name)));
            }
            if (rt.RightBomberQuota != 0) factoids.Add(string.Format("bomber quota {0:F0}%", rt.RightBomberQuota*100));
            if (rt.RightDropshipQuota != 0) factoids.Add(string.Format("dropship quota {0:F0}%", rt.RightDropshipQuota*100));
            if (rt.RightWarpQuota != 0) factoids.Add(string.Format("warp quota {0:F0}%", rt.RightWarpQuota*100));
            if (rt.RightMetalQuota != 0) factoids.Add(string.Format("metal quota {0:F0}%", rt.RightMetalQuota*100));
            if (rt.RightSetEnergyPriority) factoids.Add("can set energy priorities");
            if (rt.RightDiplomacy) factoids.Add("can control diplomacy");
            if (rt.RightEditTexts) factoids.Add("controls texts");
            return
                new MvcHtmlString(string.Format("<span title=\"<b>{0}</b><ul>{1}</ul>\"><b>{2}</b></span>",
                                                rt.Description,
                                                string.Join("", factoids.Select(x => "<li>" + x + "</li>")),
                                                rt.Name + "&nbsp"));
        }

        /// <summary>
        /// Returns the printed <see cref="Account"/>s that hold specified <see cref="RoleType"/> in the <see cref="Faction"/>
        /// </summary>
        /// <param name="rt">The <see cref="RoleType"/> whose holders should be printed</param>
        /// <param name="f">The <see cref="Faction"/> whose role holders should be printed</param>
        public static MvcHtmlString PrintFactionRoleHolders(this HtmlHelper helper, RoleType rt, Faction f) {
            List<MvcHtmlString> holders = new List<MvcHtmlString>();
            foreach (AccountRole acc in rt.AccountRoles.Where(x=>x.Account.FactionID == f.FactionID)) 
            {
                holders.Add(PrintAccount(helper, acc.Account));
            }
            return new MvcHtmlString(String.Join(", ", holders));
        }

        /// <summary>
        /// Returns the printed <see cref="Account"/>s that hold specified <see cref="RoleType"/> in the <see cref="Clan"/>
        /// </summary>
        /// <param name="rt">The <see cref="RoleType"/> whose holders should be printed</param>
        /// <param name="c">The <see cref="Clan"/> whose role holders should be printed</param>
        public static MvcHtmlString PrintClanRoleHolders(this HtmlHelper helper, RoleType rt, Clan c)
        {
            List<MvcHtmlString> holders = new List<MvcHtmlString>();
            foreach (AccountRole acc in rt.AccountRoles.Where(x => x.Account.ClanID == c.ClanID))
            {
                holders.Add(PrintAccount(helper, acc.Account));
            }
            return new MvcHtmlString(String.Join(", ", holders));
        }

        public static MvcHtmlString PrintSpringLink(this HtmlHelper helper, string link) {
           return new MvcHtmlString(string.Format("javascript:SendLobbyCommand('{0}');void(0);",link));
        }

        /// <summary>
        /// Returns a colored string that says whether the specified <see cref="PlanetStructure"/> is ACTIVE, DISABLED or POWERING
        /// </summary>
        /// <param name="s">The <see cref="PlanetStructure"/> whose status should be printed</param>
        public static MvcHtmlString PrintStructureState(this HtmlHelper helper, PlanetStructure s) {
            var url = Global.UrlHelper();
            var state = "";
            if (!s.IsActive) {
                if (s.ActivationTurnCounter == null) state = "<span style='color:red'>DISABLED</span>";
                if (s.ActivationTurnCounter >= 0) {
                    state = string.Format(" <span style='color:orange'>POWERING {0} turns left</span>", (s.TurnsToActivateOverride ?? s.StructureType.TurnsToActivate) - s.ActivationTurnCounter);
                }
            }
            else state = "<span style='color:green'>ACTIVE</span>";
            return new MvcHtmlString(state);
        }

        /// <summary>
        /// Prints a PlanetWars <see cref="StructureType"/> with tooltip
        /// </summary>
        /// <param name="stype">The <see cref="StructureType"/> to print</param>
        public static MvcHtmlString PrintStructureType(this HtmlHelper helper, StructureType stype) {
            var url = Global.UrlHelper();
            if (stype != null) return new MvcHtmlString(string.Format("<span nicetitle='$structuretype${0}'>{1}</span>", stype.StructureTypeID, stype.Name));
            else return new MvcHtmlString("");
        }

        /// <summary>
        /// Returns the specified number followed by the PlanetWars warp core icon
        /// </summary>
        public static MvcHtmlString PrintWarps(this HtmlHelper helper, double? count) {
            return new MvcHtmlString(string.Format("<span>{0}<img src='{1}' class='icon20'/></span>", count ?? 0, GlobalConst.WarpIcon));
        }

        /// <summary>
        /// <para>Returns the PlanetWars warp core icon, the number of warp cores available to the specified account, and the number of warp cores the faction as a whole has</para>
        /// <para>e.g. [warp core icon]0 / 17</para>
        /// </summary>
        public static MvcHtmlString PrintWarps(this HtmlHelper helper, Account account) {
            if (account != null && account.Faction != null) {
                var ownWarps = account.GetWarpAvailable();
                var factionWarps = account.Faction.Warps;
                return
                    new MvcHtmlString(
                        string.Format(
                            "<span nicetitle='Warp cores available to you/owned by faction'><img src='{0}' class='icon20'/>{1} / {2}</span>",
                            GlobalConst.WarpIcon,
                            Math.Floor(ownWarps),
                            Math.Floor(factionWarps)));
            }
            else return null;
        }

        /// <summary>
        /// Returns the sum of the + and - votes on all the specified <see cref="Account"/>'s forum posts
        /// </summary>
        public static MvcHtmlString PrintTotalPostRating(this HtmlHelper helper, Account account)
        {
            return new MvcHtmlString(string.Format("{0} / {1}",
                    string.Format("<font color='LawnGreen'>+{0}</font>", account.ForumTotalUpvotes),
                    string.Format("<font color='Tomato'>-{0}</font>", account.ForumTotalDownvotes)
                    ));
        }

        /// <summary>
        ///     <para>Returns the sum of the + and - votes on the specified <see cref="ForumPost"/></para>
        ///     <para>The + and - numbers serve as links to vote on the post</para>
        ///     <para>Also includes a link to cancel an existing vote</para>
        ///     <para>The tooltip displays the people who voted for each option</para>
        /// </summary>
        /// <param name="blockPost">Removes the vote links; is true if the viewer's <see cref="Account"/> is banned or has too many net downvotes</param>
        public static MvcHtmlString PrintPostRating(this HtmlHelper helper, ForumPost post, bool blockPost = false) {
            var url = Global.UrlHelper();
            bool noLink = (Global.Account == null || Global.AccountID == post.AuthorAccountID || Global.Account.Level < GlobalConst.MinLevelForForumVote || Global.Account.VotesAvailable <= 0 || blockPost);
            AccountForumVote previousVote = post.AccountForumVotes.SingleOrDefault(x => x.AccountID == Global.AccountID);
            bool upvoted = (previousVote != null && previousVote.Vote > 0);
            bool downvoted = (previousVote != null && previousVote.Vote < 0);
            bool votersVisible = (!GlobalConst.OnlyAdminsSeePostVoters || (Global.Account?.AdminLevel >= AdminLevel.Moderator));
            /*
            return new MvcHtmlString(string.Format("<input type='' name='upvote' value='{3}{0}{4}' title='Upvote'> / <input type='submit' name='downvote' value='{5}{1}{6}'> {2}",
                    string.Format("<font {0}>+{1}</font>", post.Upvotes > 0 ? "color='LawnGreen'" : "", post.Upvotes),
                    string.Format("<font {0}>-{1}</font>", post.Downvotes > 0 ? "color='Tomato'" : "", post.Downvotes),
                    previousVote != null ? string.Format("(<input type='submit' name='clearvote' value='clear'>)") : "",
                    upvoted ? "<strong>" : "",
                    upvoted ? "</strong>" : "",
                    downvoted ? "<strong>" : "",
                    downvoted ? "</strong>" : ""));
            */

            string upvote = string.Format("<{0} nicetitle='{1}'>{2}{3}{4}{5}",
                !noLink? string.Format("a href='{0}'", url.Action("VotePost", "Forum", new { forumPostID = post.ForumPostID, delta = 1 })) : "span",
                votersVisible? string.Format("$forumVotes${0}", post.ForumPostID) : "Upvote",
                upvoted ? "<strong>" : "",
                string.Format("<font {0}>+{1}</font>", post.Upvotes > 0 ? "color='LawnGreen'" : "", post.Upvotes),
                upvoted ? "</strong>" : "",
                !noLink? "</a>" : "</span>"
            );
            string downvote = string.Format("<{0} nicetitle='{1}'>{2}{3}{4}{5}",
                !noLink? string.Format("a href='{0}'", url.Action("VotePost", "Forum", new { forumPostID = post.ForumPostID, delta = -1 })) : "span",
                votersVisible? string.Format("$forumVotes${0}", post.ForumPostID) : "Downvote",
                downvoted ? "<strong>" : "",
                string.Format("<font {0}>-{1}</font>", post.Downvotes > 0 ? "color='Tomato'" : "", post.Downvotes),
                downvoted ? "</strong>" : "",
                !noLink? "</a>" : "</span>"
            );

            return new MvcHtmlString(string.Format("{0} / {1} {2}",
                    upvote,
                    downvote,
                    previousVote != null ? string.Format("(<a href='{0}'>cancel</a>)", url.Action("CancelVotePost", "Forum", new {forumPostID = post.ForumPostID})) : ""
                    ));
        }

        public static MvcHtmlString PrintMediaWikiEdit(this HtmlHelper helper, MediaWikiRecentChanges.MediaWikiEdit edit)
        {
            return new MvcHtmlString(string.Format("<a href=\"//zero-k.info/mediawiki/index.php?title={0}\">{0}</a> by {1} <small>{2}</small>",
                    edit.Title, edit.Username, edit.AgoString
                    ));
        }


        public static MvcHtmlString PrintRankProgress(this HtmlHelper helper, Account account)
        {
            var ratio =  Ratings.Ranks.GetRankProgress(account);
            int percentage = (int)Math.Round(ratio * 100);
            var progressText = string.Format("Progress to the next rank: {0}%", percentage);
            if (percentage >= 100) progressText = "Rank up on next victory!";
            var str = new MvcHtmlString(string.Format("Current rank: <img src='/img/ranks/{0}_{1}.png'  class='icon16' alt='rank' /> {2} <br /> <br /> {3}<br /> <br />Win more games to improve your rank!", account.GetIconLevel(), account.Rank, Ratings.Ranks.RankNames[account.Rank], progressText));
            return str;
        }

        /// <summary>
        /// <para>Converts strings preceded with an @ to a printed <see cref="Account"/>, <see cref="SpringBattle"/>, etc. as appropriate</para>
        /// <para>e.g. @KingRaptor becomes the printed account for user KingRaptor</para>
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string ProcessAtSignTags(string str) {
            var db = new ZkDataContext();
            str = Regex.Replace(str,
                                @"@([\w\[\]]+)",
                                m =>
                                    {
                                        var val = m.Groups[1].Value;
                                        var acc = Account.AccountByName(db, val);
                                        if (acc != null) return PrintAccount(null, acc).ToString();
                                        var clan = db.Clans.FirstOrDefault(x => x.Shortcut == val);
                                        if (clan != null) return PrintClan(null, clan).ToString();
                                        var fac = db.Factions.FirstOrDefault(x => x.Shortcut == val);
                                        if (fac != null) return PrintFaction(null, fac, false).ToString();

                                        if (val.StartsWith("b", StringComparison.InvariantCultureIgnoreCase)) {
                                            var bid = 0;
                                            if (int.TryParse(val.Substring(1), out bid)) {
                                                var bat = db.SpringBattles.FirstOrDefault(x => x.SpringBattleID == bid);
                                                if (bat != null) return PrintBattle(null, bat).ToString();
                                            }
                                        }
                                        return "@" + val;
                                    });
            return str;
        }


        public static MvcHtmlString Select(this HtmlHelper helper, string name, Type etype, int? selected, string anyItem) {
            var sb = new StringBuilder();
            sb.AppendFormat("<select name='{0}'>", helper.Encode(name));
            var names = Enum.GetNames(etype);
            var values = (int[])Enum.GetValues(etype);
            if (anyItem != null) sb.AppendFormat("<option {1}>{0}</option>", helper.Encode(anyItem), selected == null ? "selected" : "");
            for (var i = 0; i < names.Length; i++)
                sb.AppendFormat("<option value='{0}' {2}>{1}</option>",
                                helper.Encode(values[i]),
                                helper.Encode(names[i]),
                                selected == values[i] ? "selected" : "");
            sb.Append("</select>");
            return new MvcHtmlString(sb.ToString());
        }


        public static MvcHtmlString Select(this HtmlHelper helper, string name, IEnumerable<SelectOption> items, string selected) {
            var sb = new StringBuilder();
            sb.AppendFormat("<select name='{0}'>", helper.Encode(name));
            foreach (var item in items)
                sb.AppendFormat("<option value='{0}' {2}>{1}</option>",
                                helper.Encode(item.Value),
                                helper.Encode(item.Name),
                                selected == item.Value ? "selected" : "");
            sb.Append("</select>");
            return new MvcHtmlString(sb.ToString());
        }

        /// <summary>
        /// Returns the star rating of a map, mission, etc.
        /// </summary>
        /// <param name="type">Enum. Can be a red, white or green star, or a red or green skull</param>
        /// <returns></returns>
        public static MvcHtmlString Stars(this HtmlHelper helper, StarType type, double? rating) {
            if (rating.HasValue) {
                var totalWidth = 5*14;
                var starWidth = (int)(rating*14.0);
                return
                    new MvcHtmlString(string.Format("<span class='{0}' style='width:{1}px'></span><span style='width:{3}px'></span>",
                                                    type,
                                                    starWidth,
                                                    rating,
                                                    totalWidth - starWidth));
            }
            else {
                return
                    new MvcHtmlString(string.Format("<span class='{0}' style='width:70px' title='No votes'></span>",
                                                    type == StarType.RedSkull ? StarType.WhiteSkull : StarType.WhiteStarSmall));
            }
        }


        public static string ToAgoString(this DateTime? utcDate) {
            if (utcDate.HasValue) return ToAgoString(DateTime.UtcNow.Subtract(utcDate.Value));
            else return "";
        }

        public static string ToAgoString(this DateTime utcDate) {
            return ToAgoString(DateTime.UtcNow.Subtract(utcDate));
        }

        public static string ToAgoString(this TimeSpan timeSpan) {
            if (timeSpan.TotalSeconds > 0) return string.Format("{0} ago", timeSpan.Duration().ToNiceString());
            else return string.Format("in {0}", timeSpan.Duration().ToNiceString());
        }

        /// <summary>
        /// Converts a <see cref="TimeSpan"/> to "X seconds/minutes/hours/days/months ago"
        /// </summary>
        public static string ToNiceString(this TimeSpan timeSpan) {
            if (timeSpan.TotalMinutes < 2) return string.Format("{0} seconds", (int)timeSpan.TotalSeconds);
            if (timeSpan.TotalHours < 2) return string.Format("{0} minutes", (int)timeSpan.TotalMinutes);
            if (timeSpan.TotalDays < 2) return string.Format("{0} hours", (int)timeSpan.TotalHours);
            if (timeSpan.TotalDays < 60) return string.Format("{0} days", (int)timeSpan.TotalDays);
            if (timeSpan.TotalDays < 365*2) return string.Format("{0} months", (int)(timeSpan.TotalDays / 30));
            return string.Format("{0} years", (int)(timeSpan.TotalDays/365));
        }


        public static MvcHtmlString EnumCheckboxesFor<TModel, TEnum>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, IList<TEnum>>> expression, IList<TEnum> hideList = null)
        {
            var listing = (IList<TEnum>)ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData).Model;
            var name = ExpressionHelper.GetExpressionText(expression);
            var sb = new StringBuilder();
            foreach (var val in Enum.GetValues(typeof(TEnum)))
            {
                if (hideList != null && hideList.Contains((TEnum)val)) continue;
                var isSelected = listing == null;
                if (listing != null) isSelected = listing.Contains((TEnum)val);
                sb.AppendFormat("<label><input type='checkbox' name='{0}' value='{1}' {2}/>{3}</label>",
                                name,
                                (int)val,
                                isSelected ? "checked='checked'" : "",
                                Utils.Description((Enum)val));

            }

            return new MvcHtmlString(sb.ToString());
        }


        public static MvcHtmlString MultiSelectFor<TModel, TEnum>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, IList<TEnum>>> expression, string autocompleteAction, Func<TEnum, MvcHtmlString> objectRenderer)
        {
            var listing = (IList<TEnum>)ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData).Model ?? new List<TEnum>();
            var name = ExpressionHelper.GetExpressionText(expression);
            var sb = new StringBuilder();
            sb.AppendFormat("<input data-autocomplete='{0}' data-autocomplete-action='add' id='{1}' name='' type='text' value='' class='ui-autocomplete-input' autocomplete='off'><br /><div id='{2}players'></div>", autocompleteAction, name, name);
            foreach (var val in listing)
            {
                var visName = "multivis" + name + val.ToString();
                var hidName = "multihid" + name + val.ToString();
                sb.AppendFormat("<span id='{0}'>{1} <a onclick='$(\"#{2}\").remove();$(\"#{3}\").remove();'><img src='/img/delete_trashcan.png' class='icon16' /></a><br /></span><input type='hidden' name='{4}' id='{5}' value='{6}'>",
                                visName,
                                objectRenderer.Invoke(val),
                                visName,
                                hidName,
                                name,
                                hidName,
                                val);
            }

            return new MvcHtmlString(sb.ToString());
        }


        public static Account CurrentAccount(this ZkDataContext db)
        {
            if (Global.AccountID > 0 && Global.IsAccountAuthorized) return db.Accounts.Find(Global.AccountID);
            else return null;
        }

    }
}
