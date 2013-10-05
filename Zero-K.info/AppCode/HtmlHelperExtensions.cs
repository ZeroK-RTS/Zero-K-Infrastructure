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

    public static class HtmlHelperExtensions
    {
        public static MvcHtmlString AccountAvatar(this HtmlHelper helper, Account account) {
            return new MvcHtmlString(string.Format("<img src='/img/avatars/{0}.png' class='avatar'>", account.Avatar));
        }

        // todo all calls must provide helper!
        public static MvcHtmlString BBCode(this HtmlHelper helper, string str) {
            if (str == null) return null;

            str = ProcessAtSignTags(str);

            Regex exp;
            // format the bold tags: [b][/b]
            // becomes: <strong></strong>
            exp = new Regex(@"\[b\]((.|\n)+?)\[/b\]", RegexOptions.IgnoreCase);
            str = exp.Replace(str, "<strong>$1</strong>");

            // format the quote tags: [quote][/quote]
            // becomes: stuff
            exp = new Regex(@"\[quote\]((.|\n)+?)\[/quote\]", RegexOptions.IgnoreCase);
            str = exp.Replace(str,
                              "<table border=\"0\" cellpadding=\"6\" cellspacing=\"0\" width=\"100%\"><tbody><tr><td style=\"border: 1px inset;\"><em>quote:<br>$1</em></td></tr></tbody></table>");

            // format the italic tags: [i][/i]
            // becomes: <em></em>
            exp = new Regex(@"\[i\]((.|\n)+?)\[/i\]", RegexOptions.IgnoreCase);
            str = exp.Replace(str, "<em>$1</em>");

            // format the underline tags: [u][/u]
            // becomes: <u></u>
            exp = new Regex(@"\[u\]((.|\n)+?)\[/u\]", RegexOptions.IgnoreCase);
            str = exp.Replace(str, "<u>$1</u>");

            // format the strike tags: [s][/s]
            // becomes: <strike></strike>
            exp = new Regex(@"\[s\]((.|\n)+?)\[/s\]", RegexOptions.IgnoreCase);
            str = exp.Replace(str, "<strike>$1</strike>");

            // format the url tags: [url=www.website.com]my site[/url]
            // becomes: <a href="www.website.com">my site</a>
            exp = new Regex(@"\[url\=([^\]]+)\]([^\]]+)\[/url\]", RegexOptions.IgnoreCase);
            str = exp.Replace(str, "<a href=\"$1\">$2</a>");

            // format the img tags: [img]www.website.com/img/image.jpeg[/img]
            // becomes: <img src="www.website.com/img/image.jpeg" />
            exp = new Regex(@"\[img\]([^\[]+)\[/img\]", RegexOptions.IgnoreCase);
            str = exp.Replace(str, "<img src=\"$1\" />");

            // format img tags with alt: [img=www.website.com/img/image.jpeg]this is the alt text[/img]
            // becomes: <img src="www.website.com/img/image.jpeg" alt="this is the alt text" />
            exp = new Regex(@"\[img\=([^\]]+)\]([^\]]+)\[/img\]", RegexOptions.IgnoreCase);
            str = exp.Replace(str, "<img src=\"$1\" alt=\"$2\" />");

            //format the colour tags: [color=red][/color]
            // becomes: <font color="red"></font>
            // supports UK English and US English spelling of colour/color
            exp = new Regex(@"\[color\=([^\]]+)\]([^\]]+)\[/color\]", RegexOptions.IgnoreCase);
            str = exp.Replace(str, "<font color=\"$1\">$2</font>");
            exp = new Regex(@"\[colour\=([^\]]+)\]([^\]]+)\[/colour\]", RegexOptions.IgnoreCase);
            str = exp.Replace(str, "<font color=\"$1\">$2</font>");

            // format the size tags: [size=3][/size]
            // becomes: <font size="+3"></font>
            exp = new Regex(@"\[size\=([^\]]+)\]([^\]]+)\[/size\]", RegexOptions.IgnoreCase);
            str = exp.Replace(str, "<font size=\"+$1\">$2</font>");

            str = Regex.Replace(str, @"(^|[\s])((mailto|spring|http|https|ftp|ftps)\://\S+)", @"$1<a href='$2'>$2</a>");

            // lastly, replace any new line characters with <br />
            str = str.Replace("\r\n", "<br />\r\n");

            if (helper != null) {
                // todo remove condition in the future
                exp = new Regex(@"\[poll\]([0-9]+)\[/poll\]", RegexOptions.IgnoreCase);
                str = exp.Replace(str, m => helper.Action("Index", "Poll", new { pollID = m.Groups[1].Value }).ToHtmlString());
            }

            return new MvcHtmlString(str);
        }

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
                var path = HttpContext.Current.Server.MapPath(name);
                return new MvcHtmlString(File.ReadAllText(path));
            }
        }


        public static MvcHtmlString IncludeWiki(this HtmlHelper helper, string node) {
            return new MvcHtmlString(WikiHandler.LoadWiki(node, "", true));
        }

        public static MvcHtmlString Print(this HtmlHelper helper, ForumThread thread) {
            var url = new UrlHelper(HttpContext.Current.Request.RequestContext);
            var link = url.Action("Thread", "Forum", new { id = thread.ForumThreadID });
            link = string.Format("<a href='{0}' title='$thread${1}'>", link, thread.ForumThreadID);
            var lastRead = thread.ForumThreadLastReads.FirstOrDefault(x => x.AccountID == Global.AccountID);

            string format;

            if (lastRead == null) format = "<span>{0}<img src='/img/mail/mail-unread.png' height='15' /><i>{1}</i></a></span>";
            else {
                if (lastRead.LastRead >= thread.LastPost) format = "<span>{0}<img src='/img/mail/mail-read.png' height='15' />{1}</a></span>";
                else {
                    if (lastRead.LastPosted != null) format = "<span>{0}<img src='/img/mail/mail-new.png' height='15' /><b>{1}</b></a></span>";
                    else format = "<span>{0}<img src='/img/mail/mail-unread.png' height='15' />{1}</a></span>";
                }
            }

            return new MvcHtmlString(string.Format(format, link, thread.Title));
        }

        public static MvcHtmlString PrintAccount(this HtmlHelper helper, Account account, bool colorize = true) {
            if (account == null) return new MvcHtmlString("Nobody");
            else {
                var clanStr = "";
                var url = new UrlHelper(HttpContext.Current.Request.RequestContext);
                if (account.Clan != null) {
                    clanStr = string.Format("<a href='{1}' nicetitle='$clan${2}'><img src='{0}' width='16'/></a>",
                                            account.Clan.GetImageUrl(),
                                            url.Action("Detail", "Clans", new { id = account.ClanID }),
                                            account.ClanID);
                }
                else if (account.Faction != null) clanStr = string.Format("<img src='{0}' width='16'/>", account.Faction.GetImageUrl());
                var adminStr = "";
                if (account.IsZeroKAdmin) adminStr = "<img src='/img/police.png'  class='icon16' alt='Admin' />";

                var clampedLevel = account.Level/10 + 1;
                if (clampedLevel < 1) clampedLevel = 1;
                if (clampedLevel > 9) clampedLevel = 9;

                string color = Faction.FactionColor(account.Faction, Global.FactionID);
                if (String.IsNullOrEmpty(color)) color = "#B0D0C0";

                return
                    new MvcHtmlString(
                        string.Format(
                            "<img src='/img/flags/{0}.png' class='flag' height='11' width='16' alt='{0}'/><img src='/img/ranks/{1}.png'  class='icon16' alt='rank' />{5}{6}<a href='/Users/Detail/{2}' style='color:{3}' nicetitle='$user${2}'>{4}</a>",
                            account.Country != "??" ? account.Country : "unknown",
                            clampedLevel,
                            account.AccountID,
                            colorize ? color : "",
                            account.Name,
                            clanStr,
                            adminStr));
            }
        }


        public static MvcHtmlString PrintBattle(this HtmlHelper helper, SpringBattlePlayer battlePlayer) {
            if (battlePlayer == null) return null;
            return PrintBattle(helper, battlePlayer.SpringBattle, battlePlayer.IsSpectator ? null : (bool?)battlePlayer.IsInVictoryTeam);
        }

        public static MvcHtmlString PrintBattle(this HtmlHelper helper, SpringBattle battle, bool? isVictory = null) {
            var url = new UrlHelper(HttpContext.Current.Request.RequestContext);
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
                                                PrintMap(helper, battle.ResourceByMapResourceID.InternalName),
                                                icon));
        }

        public static MvcHtmlString PrintBombers(this HtmlHelper helper, double? count) {
            return new MvcHtmlString(string.Format("<span>{0}<img src='{1}' class='icon20'/></span>", count ?? 0, GlobalConst.BomberIcon));
        }

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


        public static MvcHtmlString PrintClan(this HtmlHelper helper, Clan clan, bool colorize = true) {
            var url = new UrlHelper(HttpContext.Current.Request.RequestContext);
            if (clan == null) return new MvcHtmlString(string.Format("<a href='{0}'>No Clan</a>", url.Action("Index", "Clans")));
            {
                string color = Clan.ClanColor(clan, Global.ClanID);
                if (String.IsNullOrEmpty(color)) color = "#B0D0C0";
                return
                    new MvcHtmlString(
                        string.Format("<a href='{0}' nicetitle='$clan${4}'><img src='{1}' width='16'><span style='color:{2}'>{3}</span></a>",
                                      url.Action("Detail", "Clans", new { id = clan.ClanID }),
                                      clan.GetImageUrl(),
                                      colorize ? color : "",
                                      clan.Shortcut,
                                      clan.ClanID));
            }
        }

        public static MvcHtmlString PrintContributorStar(this HtmlHelper helper, Account account, bool large = false) {
            var star = "";
            var contribs = account.ContributionsByAccountID.ToList();
            var total = 0;
            foreach (var contrib in contribs) total += contrib.KudosValue;
            if (total >= GlobalConst.KudosForGold) star = "star_yellow";
            else if (total >= GlobalConst.KudosForSilver) star = "star_white";
            else if (total >= GlobalConst.KudosForBronze) star = "star_brown";
            else return new MvcHtmlString("");

            if (large == false) star = star + "_small";
            return new MvcHtmlString(string.Format("<img src='/img/stars/{0}.png' alt='Donator star'/>", star));
        }

        public static MvcHtmlString PrintDropships(this HtmlHelper helper, double? count, Faction faction) {
            return
                new MvcHtmlString(string.Format("<span>{0}<img src='{1}' class='icon20'/></span>", Math.Floor(count ?? 0), faction.GetShipImageUrl()));
        }

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

        public static MvcHtmlString PrintEnergy(this HtmlHelper helper, double? count) {
            return new MvcHtmlString(string.Format("<span>{0}<img src='{1}' class='icon20'/></span>", Math.Floor(count ?? 0), GlobalConst.EnergyIcon));
        }

        public static MvcHtmlString PrintFaction(this HtmlHelper helper, Faction fac, bool big = true) {
            var url = new UrlHelper(HttpContext.Current.Request.RequestContext);
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


        public static MvcHtmlString PrintFactionTreaty(this HtmlHelper helper, FactionTreaty treaty) {
            var url = new UrlHelper(HttpContext.Current.Request.RequestContext);
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

        public static MvcHtmlString PrintInfluence(this HtmlHelper helper, Faction fac, double influence) {
            var formattedString = string.Format("<span style='color:{0}'>{1:0.#}%</span>", Faction.FactionColor(fac, Global.FactionID), influence);
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
            var url = new UrlHelper(HttpContext.Current.Request.RequestContext);
            return new MvcHtmlString(string.Format("<a href='{0}' title='$map${1}'>{1}</a>", url.Action("DetailName", "Maps", new { name }), name));
        }

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


        public static MvcHtmlString PrintMetal(this HtmlHelper helper, double? cost) {
            return
                new MvcHtmlString(string.Format("<span style='color:#00FFFF;'>{0}<img src='{1}' class='icon20'/></span>",
                                                Math.Floor(cost ?? 0),
                                                GlobalConst.MetalIcon));
        }

        public static MvcHtmlString PrintPlanet(this HtmlHelper helper, Planet planet) {
            if (planet == null) return new MvcHtmlString("?");
            var url = new UrlHelper(HttpContext.Current.Request.RequestContext);
            return
                new MvcHtmlString(string.Format("<a href='{0}' title='$planet${4}' style='{5}'><img src='/img/planets/{1}' width='{2}'>{3}</a>",
                                                url.Action("Planet", "Planetwars", new { id = planet.PlanetID }),
                                                planet.Resource.MapPlanetWarsIcon,
                                                planet.Resource.PlanetWarsIconSize/3,
                                                planet.Name,
                                                planet.PlanetID,
                                                planet.Faction != null ? "color:" + planet.Faction.Color : ""));
        }

        public static MvcHtmlString PrintPlanet(this HtmlHelper helper, CampaignPlanet planet) {
            if (planet == null) return new MvcHtmlString("?");
            var db = new ZkDataContext();
            var url = new UrlHelper(HttpContext.Current.Request.RequestContext);
            var map = db.Resources.FirstOrDefault(m => m.InternalName == planet.Mission.Map);
            return
                new MvcHtmlString(string.Format("<a href='{0}' title='$planet${4}'><img src='/img/planets/{1}' width='{2}'>{3}</a>",
                                                url.Action("Planet", "Campaign", new { id = planet.PlanetID }),
                                                map.MapPlanetWarsIcon,
                                                map.PlanetWarsIconSize/3,
                                                planet.Name,
                                                planet.PlanetID));
        }

        public static MvcHtmlString PrintRoleType(this HtmlHelper helper, RoleType rt) {
            var factoids = new List<string>();
            if (rt.IsClanOnly) factoids.Add("clan based");
            if (rt.IsOnePersonOnly) factoids.Add("only one person can hold this");

            if (rt.IsVoteable) factoids.Add("is voteable");
            if (rt.RoleTypeHierarchiesByMasterRoleTypeID.Any(x => x.CanAppoint)) {
                factoids.Add("appoints: " +
                             string.Join(", ",
                                         rt.RoleTypeHierarchiesByMasterRoleTypeID.Where(x => x.CanAppoint)
                                           .Select(x => x.RoleTypeBySlaveRoleTypeID.Name)));
            }
            if (rt.RoleTypeHierarchiesByMasterRoleTypeID.Any(x => x.CanRecall)) {
                factoids.Add("recalls: " +
                             string.Join(", ",
                                         rt.RoleTypeHierarchiesByMasterRoleTypeID.Where(x => x.CanRecall)
                                           .Select(x => x.RoleTypeBySlaveRoleTypeID.Name)));
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

        public static MvcHtmlString PrintFactionRoleHolders(this HtmlHelper helper, RoleType rt, Faction f) {
            List<MvcHtmlString> holders = new List<MvcHtmlString>();
            foreach (AccountRole acc in rt.AccountRoles.Where(x=>x.AccountByAccountID.FactionID == f.FactionID)) 
            {
                holders.Add(PrintAccount(helper, acc.AccountByAccountID));
            }
            return new MvcHtmlString(String.Join(", ", holders));
        }

        public static MvcHtmlString PrintClanRoleHolders(this HtmlHelper helper, RoleType rt, Clan c)
        {
            List<MvcHtmlString> holders = new List<MvcHtmlString>();
            foreach (AccountRole acc in rt.AccountRoles.Where(x => x.AccountByAccountID.ClanID == c.ClanID))
            {
                holders.Add(PrintAccount(helper, acc.AccountByAccountID));
            }
            return new MvcHtmlString(String.Join(", ", holders));
        }

        public static MvcHtmlString PrintSpringLink(this HtmlHelper helper, string link) {
           return new MvcHtmlString(string.Format("javascript:SendLobbyCommand('{0}');void(0);",link));
        }

        public static MvcHtmlString PrintStructureState(this HtmlHelper helper, PlanetStructure s) {
            var url = new UrlHelper(HttpContext.Current.Request.RequestContext);
            var state = "";
            if (!s.IsActive) {
                if (s.ActivatedOnTurn == null) state = "<span style='color:red'>DISABLED</span>";
                if (s.ActivatedOnTurn != null) {
                    state = string.Format(" <span style='color:orange'>POWERING {0} turns left</span>",
                                          s.StructureType.TurnsToActivate - s.Planet.Galaxy.Turn + s.ActivatedOnTurn);
                }
            }
            else state = "<span style='color:green'>ACTIVE</span>";
            return new MvcHtmlString(state);
        }

        public static MvcHtmlString PrintStructureType(this HtmlHelper helper, StructureType stype) {
            var url = new UrlHelper(HttpContext.Current.Request.RequestContext);
            if (stype != null) return new MvcHtmlString(string.Format("<span nicetitle='$structuretype${0}'>{1}</span>", stype.StructureTypeID, stype.Name));
            else return new MvcHtmlString("");
        }

        public static MvcHtmlString PrintWarps(this HtmlHelper helper, double? count) {
            return new MvcHtmlString(string.Format("<span>{0}<img src='{1}' class='icon20'/></span>", count ?? 0, GlobalConst.WarpIcon));
        }

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
        public static MvcHtmlString PrintTotalPostRating(this HtmlHelper helper, Account account)
        {
            return new MvcHtmlString(string.Format("{0} / {1}",
                    string.Format("<font color='LawnGreen'>+{0}</font>", account.ForumTotalUpvotes),
                    string.Format("<font color='Tomato'>-{0}</font>", account.ForumTotalDownvotes)
                    ));
        }

        public static MvcHtmlString PrintPostRating(this HtmlHelper helper, ForumPost post, bool blockPost) {
            var url = new UrlHelper(HttpContext.Current.Request.RequestContext);
            bool noLink = (Global.Account == null || Global.AccountID == post.AuthorAccountID || Global.Account.Level < GlobalConst.MinLevelForForumVote || Global.Account.VotesAvailable <= 0 || blockPost);
            AccountForumVote previousVote = post.AccountForumVotes.SingleOrDefault(x => x.AccountID == Global.AccountID);
            bool upvoted = (previousVote != null && previousVote.Vote > 0);
            bool downvoted = (previousVote != null && previousVote.Vote < 0);
            bool votersVisible = (!GlobalConst.OnlyAdminsSeePostVoters || (Global.Account != null && Global.Account.IsZeroKAdmin));
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
            return string.Format("{0} ago", timeSpan.ToNiceString());
        }

        public static string ToNiceString(this TimeSpan timeSpan) {
            if (timeSpan.TotalMinutes < 2) return string.Format("{0} seconds", (int)timeSpan.TotalSeconds);
            if (timeSpan.TotalHours < 2) return string.Format("{0} minutes", (int)timeSpan.TotalMinutes);
            if (timeSpan.TotalDays < 2) return string.Format("{0} hours", (int)timeSpan.TotalHours);
            if (timeSpan.TotalDays < 60) return string.Format("{0} days", (int)timeSpan.TotalDays);
            return string.Format("{0} months", (int)(timeSpan.TotalDays/30));
        }
    }
}