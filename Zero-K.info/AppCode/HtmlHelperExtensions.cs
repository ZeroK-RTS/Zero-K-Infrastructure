using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Mvc.Html;
using System.Web.WebPages;
using PlasmaShared;
using ZeroKWeb;
using ZeroKWeb.ForumParser;
using ZkData;

namespace System.Web.Mvc
{

    public class SelectOption
    {
        public string Name;
        public string Value;
    }

    /// <summary>
    /// <para>Contains functions that return a <see cref="MvcHtmlString"/> for pretty display of things like accounts and clans</para>
    /// <para>The returned string is often also a link leading to the appropriate page, and has its own tooltip</para>
    /// </summary>
    public static partial class HtmlHelperExtensions
    {
        public static MvcHtmlString AccountAvatar(this HtmlHelper helper, Account account) {
            if (account.IsDeleted) return null;
            return new MvcHtmlString(string.Format("<img src='/img/{0}/{1}.png' class='avatar'>", GlobalConst.AvatarFolder, account.Avatar));
        }

        /// <summary>
        /// Uses regexes to turn a string into BBcode
        /// </summary>
        public static MvcHtmlString BBCode(this HtmlHelper helper, string str) {
            if (str == null) return null;
            return new MvcHtmlString(new ForumWikiParser().TranslateToHtml(str, helper));
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










        /// <summary>
        /// Returns an appropriately formatted link with username and relevant user icons leading to an account page
        /// </summary>
        /// <param name="account">Account to print</param>
        /// <param name="colorize">If true, write the user name in <see cref="Faction"/> color</param>
        /// <param name="ignoreDeleted">If false, just prints "{redacted}" for accounts marked as deleted</param>
        /// <summary>
        /// The viewer and the URL builder the formatters used to read off Global directly. Passing
        /// them keeps every existing caller's output identical - including the URLs, which this
        /// still generates through MVC 5's own UrlHelper rather than assembling by hand.
        /// </summary>
        private static ZkHtmlContext WebContext() => new ZkHtmlContext
        {
            Action = (action, controller, values) => Global.UrlHelper().Action(action, controller, values),
            ViewerFactionID = Global.FactionID,
            ViewerClanID = Global.ClanID,
            ViewerIsModerator = Global.IsModerator,
        };

        /// <summary>Delegates to the one implementation in ZkData/ZkHtmlFormat.cs.</summary>
        public static MvcHtmlString PrintAccount(this HtmlHelper helper, Account account, bool colorize = true, bool ignoreDeleted = false, bool makeLinks = true)
            => new MvcHtmlString(ZkHtmlFormat.PrintAccount(WebContext(), account, colorize, ignoreDeleted, makeLinks));

        public static MvcHtmlString PrintDate(this HtmlHelper helper, DateTime? dateTime) {
            return new MvcHtmlString($"<span nicetitle=\"{dateTime}\">{dateTime.ToAgoString()}</span>");    
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
        /// <summary>Delegates to the one implementation in ZkData/ZkHtmlFormat.cs.</summary>
        public static MvcHtmlString PrintClan(this HtmlHelper helper, Clan clan, bool colorize = true, bool big = false)
            => new MvcHtmlString(ZkHtmlFormat.PrintClan(WebContext(), clan, colorize, big));


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
        /// <summary>Delegates to the one implementation in ZkData/ZkHtmlFormat.cs.</summary>
        public static MvcHtmlString PrintFaction(this HtmlHelper helper, Faction fac, bool big = true)
            => new MvcHtmlString(ZkHtmlFormat.PrintFaction(WebContext(), fac, big));

        /// <summary>
        /// Returns a PlanetWars treaty ID with link
        /// </summary>
        /// <summary>Delegates to the one implementation in ZkData/ZkHtmlFormat.cs.</summary>
        public static MvcHtmlString PrintFactionTreaty(this HtmlHelper helper, FactionTreaty treaty)
            => new MvcHtmlString(ZkHtmlFormat.PrintFactionTreaty(WebContext(), treaty));


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

            // Encoded, because a map's InternalName is not restricted. Resource.InternalName has
            // [Required] and [StringLength(255)] and nothing else, RegisterResource only null-checks
            // it, and the name comes out of an uploaded map archive - so it is user-supplied text
            // going into an attribute and into element content. Compare PrintAccount, which does not
            // encode and does not need to: Account.IsValidLobbyName is enforced server-side at both
            // registration and rename, so a player name cannot carry markup.
            //
            // url.Action encodes the query string itself, so only the two literal positions matter.
            var encoded = System.Net.WebUtility.HtmlEncode(name);
            return new MvcHtmlString(string.Format("<a href='{0}' title='$map${1}'>{1}</a>", url.Action("DetailName", "Maps", new { name }), encoded));
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
        /// <summary>Delegates to the one implementation in ZkData/ZkHtmlFormat.cs.</summary>
        public static MvcHtmlString PrintPlanet(this HtmlHelper helper, Planet planet)
            => new MvcHtmlString(ZkHtmlFormat.PrintPlanet(WebContext(), planet));


        /// <summary>
        /// Returns the clan/faction role name and tooltip
        /// </summary>
        /// <param name="rt">The <see cref="RoleType"/> to print</param>
        /// <summary>Delegates to the one implementation in ZkData/ZkHtmlFormat.cs.</summary>
        public static MvcHtmlString PrintRoleType(this HtmlHelper helper, RoleType rt)
            => new MvcHtmlString(ZkHtmlFormat.PrintRoleType(WebContext(), rt));









        /// <summary>
        /// Prints a PlanetWars <see cref="StructureType"/> with tooltip
        /// </summary>
        /// <param name="stype">The <see cref="StructureType"/> to print</param>
        /// <summary>Delegates to the one implementation in ZkData/ZkHtmlFormat.cs.</summary>
        public static MvcHtmlString PrintStructureType(this HtmlHelper helper, StructureType stype)
            => new MvcHtmlString(ZkHtmlFormat.PrintStructureType(WebContext(), stype));

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





        public static MvcHtmlString PrintMediaWikiEdit(this HtmlHelper helper, MediaWikiRecentChanges.MediaWikiEdit edit)
        {
            // Title and Username come from the wiki's recent-changes feed - written by whoever
            // edits the wiki - and went into both an href and the page unencoded. The href needs
            // URL encoding and the text needs HTML encoding; they are different jobs and the
            // original did neither.
            return new MvcHtmlString(string.Format("<a href=\"//zero-k.info/mediawiki/index.php?title={0}\">{1}</a> by {2} <small>{3}</small>",
                    System.Uri.EscapeDataString(edit.Title ?? ""),
                    System.Net.WebUtility.HtmlEncode(edit.Title),
                    System.Net.WebUtility.HtmlEncode(edit.Username),
                    System.Net.WebUtility.HtmlEncode(edit.AgoString)
                    ));
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





        /// <summary>
        /// Converts a <see cref="TimeSpan"/> to "X seconds/minutes/hours/days/months ago"
        /// </summary>


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
        


    }
}
