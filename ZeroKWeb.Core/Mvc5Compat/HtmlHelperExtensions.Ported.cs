using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using PlasmaShared;
using ZeroKWeb;
using ZkData;

// MvcHtmlString, not IHtmlContent, and that is deliberate.
//
// MVC 5's helpers all return MvcHtmlString. These were first written returning IHtmlContent,
// which is the ASP.NET Core idiom and is what the views need - but it is NOT what the MVC 5
// signature is, and the difference only shows when LINKED code stores a result:
//
//     List<MvcHtmlString> holders = new List<MvcHtmlString>();
//     holders.Add(PrintAccount(helper, acc.Account));    // CS1503 on .NET 9 only
//
// PrintFactionRoleHolders does exactly that, and it is shared source, so it has to compile
// against both. MvcHtmlString IS an IHtmlContent - it subclasses HtmlString - so matching the
// MVC 5 signature costs nothing and every existing caller is unaffected.

namespace System.Web.Mvc
{
    /// <summary>
    /// The first tranche of Zero-K.info's view helpers, rewritten for ASP.NET Core.
    ///
    /// This is the first part of the view port that is neither a file move nor a shim.
    /// HtmlHelperExtensions.cs is 837 lines against MVC 5's HtmlHelper returning
    /// MvcHtmlString; neither type exists here, so these have to be written again rather
    /// than linked - which means they can drift from the originals, silently, in the HTML
    /// the site is made of.
    ///
    /// So they are transcribed literally, format string for format string, and checked
    /// against the original's exact output by ZeroKWeb.Render. A helper that is "obviously
    /// the same" but emits class='icon20' where the original wrote width='20' height='20'
    /// is a defect nothing else here would catch.
    ///
    /// Transcribing ten of these by eye produced two defects: PrintWarps(double?) gained a
    /// Math.Floor its original does not have, and PrintMetal(double?) lost the
    /// style='color:#00FFFF;' its original does have. Both were caught by reading the source
    /// again rather than by any test, which is the honest account of how much care this
    /// needs - and why the checks below assert the exact bytes.
    ///
    /// Started with the pure formatters - the ones whose whole body is a string.Format over
    /// their arguments. The hard ones (PrintAccount, BBCodeCached, the Ajax helpers) need
    /// decisions rather than transcription and are not here.
    ///
    /// MvcHtmlString became HtmlString: both mean "already-encoded HTML, do not escape".
    /// Returning null is kept where the original returns null - Razor renders nothing for
    /// it, in both frameworks.
    ///
    /// The namespace is System.Web.Mvc for the reason given in
    /// HtmlHelperExtensions.Portable.cs: it is how the views already find these.
    /// </summary>
    // A partial of the SAME class Zero-K.info/AppCode/HtmlHelperExtensions.Portable.cs
    // declares, not a separate one. The original is invoked both ways - Html.PrintAccount(x)
    // from views, HtmlHelperExtensions.PrintAccount(null, x) from PlanetwarsEventCreator - and
    // only a partial of the real class satisfies both.
    public static partial class HtmlHelperExtensions
    {

        /// <summary>
        /// The name, flag, rank, clan and moderator badge of an account - the single most
        /// used helper on the site, and the one LoginBar needs before the site layout can
        /// render.
        ///
        /// Transcribed literally, including the parts that look like mistakes and are not:
        /// two spaces before class='icon16' in the rank and admin images, an empty colour
        /// when colorize is false rather than the colour omitted, and the user link built
        /// from a hand-written "/Users/Detail/{0}" rather than from routing, which is why it
        /// does not need the UrlHelper the clan link does.
        /// </summary>
        /// <summary>Same context, built from this stack's helper rather than MVC 5's ambient.</summary>
        private static ZkHtmlContext WebContext(IHtmlHelper helper) => new ZkHtmlContext
        {
            Action = (action, controller, values) => Url(helper).Action(action, controller, values),
            ViewerFactionID = Global.FactionID,
            ViewerClanID = Global.ClanID,
            ViewerIsModerator = Global.IsModerator,
        };

        /// <summary>Delegates to the one implementation in ZkData/ZkHtmlFormat.cs.</summary>
        public static MvcHtmlString PrintAccount(this IHtmlHelper helper, Account account, bool colorize = true, bool ignoreDeleted = false, bool makeLinks = true)
            => new MvcHtmlString(ZkHtmlFormat.PrintAccount(WebContext(helper), account, colorize, ignoreDeleted, makeLinks));


        public static MvcHtmlString AccountAvatar(this IHtmlHelper helper, Account account)
        {
            // no null check on account in the original - it dereferences straight away
            if (account.IsDeleted) return null;
            return new MvcHtmlString(string.Format("<img src='/img/{0}/{1}.png' class='avatar'>", GlobalConst.AvatarFolder, account.Avatar));
        }

        public static MvcHtmlString PrintBadges(this IHtmlHelper helper, Account account,
            int? maxWidth = null, bool newlines = true)
        {
            if (account == null || account.IsDeleted) return new MvcHtmlString("");
            var badges = account.GetBadges();
            return new MvcHtmlString(string.Join("\n", badges.Select(x =>
                $"<img src='/img/badges/{x}.png' nicetitle='{x.Description()}' {(maxWidth != null ? $"style='width:{maxWidth}px;'" : "")}/>{(newlines ? "<br/>" : "")}")));
        }

        public static MvcHtmlString PrintInfluence(this IHtmlHelper helper, PlanetFaction planetFaction)
            => PrintInfluence(helper, planetFaction.Faction, planetFaction.Influence);

        public static MvcHtmlString PrintInfluence(this IHtmlHelper helper, Faction fac, double influence)
            => new MvcHtmlString(string.Format("<span style='color:{0}'>{1:0.#} ({2:0.#}%)</span>",
                Faction.FactionColor(fac, Global.FactionID), influence,
                100 * influence / GlobalConst.PlanetWarsMaximumIP));

        public static MvcHtmlString PrintInfluence(this IHtmlHelper helper, Faction faction, int influence, int shadowInfluence)
        {
            // the original builds the format string conditionally, and the non-breaking
            // spaces are written as &nbsp without the semicolon - kept as found
            var formatString = "<span style='color:{0}'>{1}</span>";
            if (shadowInfluence > 0) formatString += "&nbsp({2}&nbsp+&nbsp<span style='color:gray'>{3}</span>)";
            return new MvcHtmlString(string.Format(formatString, faction.Color,
                influence + shadowInfluence, influence, shadowInfluence));
        }

        public static MvcHtmlString Stars(this IHtmlHelper helper, StarType type, double? rating)
        {
            if (rating.HasValue)
            {
                var totalWidth = 5 * 14;
                var starWidth = (int)(rating * 14.0);
                // {2} is passed and never used by the format string - kept, since removing
                // an unused argument is still a change to a literal being transcribed
                return new MvcHtmlString(string.Format(
                    "<span class='{0}' style='width:{1}px'></span><span style='width:{3}px'></span>",
                    type, starWidth, rating, totalWidth - starWidth));
            }
            return new MvcHtmlString(string.Format("<span class='{0}' style='width:70px' title='No votes'></span>",
                type == StarType.RedSkull ? StarType.WhiteSkull : StarType.WhiteStarSmall));
        }


        /// <summary>
        /// The forum/wiki BBCode renderer. The parser itself - 40 files, 2000 lines under
        /// Zero-K.info/ForumParser - is linked rather than rewritten: it takes an HtmlHelper
        /// only to pass it to five helper calls, and a global using alias maps that name onto
        /// IHtmlHelper so the whole subsystem compiles unedited.
        ///
        /// BBCodeCached is not here. It reads Global.ForumPostCache, which is the web
        /// project's cache object rather than anything about rendering.
        /// </summary>
        public static MvcHtmlString BBCode(this IHtmlHelper helper, string str)
        {
            if (str == null) return null;
            return new MvcHtmlString(new ZeroKWeb.ForumParser.ForumWikiParser().TranslateToHtml(str, helper));
        }


        /// <summary>Verbatim from HtmlHelperExtensions.cs; Home/HomeIndex.cshtml is the only caller.</summary>
        public static MvcHtmlString PrintMediaWikiEdit(this IHtmlHelper helper, MediaWikiRecentChanges.MediaWikiEdit edit)
        {
            // Title and Username come from the wiki's recent-changes feed - written by whoever
            // edits the wiki - and went into both an href and the page unencoded. The href needs
            // URL encoding and the text needs HTML encoding; they are different jobs and the
            // original did neither. HtmlHelperExtensions.cs got this in 4d1be43d1; this copy did not.
            return new MvcHtmlString(string.Format("<a href=\"//zero-k.info/mediawiki/index.php?title={0}\">{1}</a> by {2} <small>{3}</small>",
                    System.Uri.EscapeDataString(edit.Title ?? ""),
                    System.Net.WebUtility.HtmlEncode(edit.Title),
                    System.Net.WebUtility.HtmlEncode(edit.Username),
                    System.Net.WebUtility.HtmlEncode(edit.AgoString)
                    ));
        }

        public static MvcHtmlString PrintMap(this IHtmlHelper helper, string name)
        {
            // Encoded, because a map's InternalName is not restricted. Resource.InternalName has
            // [Required] and [StringLength(255)] and nothing else, RegisterResource only null-checks
            // it, and the name comes out of an uploaded map archive - so it is user-supplied text
            // going into an attribute and into element content. Compare PrintAccount, which does not
            // encode and does not need to: Account.IsValidLobbyName is enforced server-side at both
            // registration and rename, so a player name cannot carry markup.
            //
            // Url(helper).Action encodes the query string itself, so only the two literal positions
            // matter. HtmlHelperExtensions.cs got this in 4d1be43d1 and this copy did not, because
            // the two are twins and nothing compared them - tools/check-ported-helpers.py now does.
            var encoded = System.Net.WebUtility.HtmlEncode(name);
            return new MvcHtmlString(string.Format("<a href='{0}' title='$map${1}'>{1}</a>",
                Url(helper).Action("DetailName", "Maps", new { name }), encoded));
        }

        public static MvcHtmlString PrintBattle(this IHtmlHelper helper, SpringBattlePlayer battlePlayer)
        {
            if (battlePlayer == null) return null;
            return PrintBattle(helper, battlePlayer.SpringBattle,
                battlePlayer.IsSpectator ? null : (bool?)battlePlayer.IsInVictoryTeam);
        }

        public static MvcHtmlString PrintBattle(this IHtmlHelper helper, SpringBattle battle, bool? isVictory = null)
        {
            var icon = "";
            if (isVictory == true) icon = "battlewon.png";
            else if (isVictory == null) icon = "spec.png";
            else icon = "battlelost.png";

            icon = string.Format("<img src='/img/battles/{0}' class='vcenter' />", icon);

            if (battle.IsMission) icon += " <img src='/img/battles/mission.png' alt='Mission' class='vcenter' />";
            if (battle.HasBots) icon += " <img src='/img/battles/robot.png' alt='Bots' class='vcenter' />";

            if (battle.BattleType == "Multiplayer")
                icon += " <img src='/img/battles/multiplayer.png' alt='Multiplayer' class='vcenter' />";
            else if (battle.BattleType == "Singleplayer")
                icon += " <img src='/img/battles/singleplayer.png' alt='Singleplayer' class='vcenter' />";

            // PrintMap returns HTML that is interpolated into this format string, so it has to
            // be rendered to a string here rather than handed over as IHtmlContent.
            var map = Render(PrintMap(helper, battle.ResourceByMapResourceID?.InternalName));

            return new MvcHtmlString(string.Format("<span><a href='{0}'>{4} B{1}</a> {2} on {3}</span>",
                Url(helper).Action("Detail", "Battles", new { id = battle.SpringBattleID }),
                battle.SpringBattleID, battle.PlayerCount, map, icon));
        }

        private static string Render(IHtmlContent content)
        {
            if (content == null) return null;
            using (var writer = new System.IO.StringWriter())
            {
                content.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
                return writer.ToString();
            }
        }


        /// <summary>Delegates to the one implementation in ZkData/ZkHtmlFormat.cs.</summary>
        public static MvcHtmlString PrintFactionTreaty(this IHtmlHelper helper, FactionTreaty treaty)
            => new MvcHtmlString(ZkHtmlFormat.PrintFactionTreaty(WebContext(helper), treaty));

        /// <summary>Delegates to the one implementation in ZkData/ZkHtmlFormat.cs.</summary>
        public static MvcHtmlString PrintPlanet(this IHtmlHelper helper, Planet planet)
            => new MvcHtmlString(ZkHtmlFormat.PrintPlanet(WebContext(helper), planet));

        /// <summary>Delegates to the one implementation in ZkData/ZkHtmlFormat.cs.</summary>
        public static MvcHtmlString PrintStructureType(this IHtmlHelper helper, StructureType stype)
            => new MvcHtmlString(ZkHtmlFormat.PrintStructureType(WebContext(helper), stype));

        /// <summary>Delegates to the one implementation in ZkData/ZkHtmlFormat.cs.</summary>
        public static MvcHtmlString PrintRoleType(this IHtmlHelper helper, RoleType rt)
            => new MvcHtmlString(ZkHtmlFormat.PrintRoleType(WebContext(helper), rt));

        public static MvcHtmlString PrintDate(this IHtmlHelper helper, DateTime? dateTime)
            => new MvcHtmlString($"<span nicetitle=\"{dateTime}\">{dateTime.ToAgoString()}</span>");

        public static MvcHtmlString PrintEnergy(this IHtmlHelper helper, double? count)
            => new MvcHtmlString(string.Format("<span>{0}<img src='{1}' class='icon20'/></span>",
                Math.Floor(count ?? 0), GlobalConst.EnergyIcon));

        public static MvcHtmlString PrintMetal(this IHtmlHelper helper, double? cost)
            // the span really does carry a style here and not on the others
            => new MvcHtmlString(string.Format("<span style='color:#00FFFF;'>{0}<img src='{1}' class='icon20'/></span>",
                Math.Floor(cost ?? 0), GlobalConst.MetalIcon));

        public static MvcHtmlString PrintMetal(this IHtmlHelper helper, Account account)
        {
            if (account == null || account.Faction == null) return null;
            // width/height here, class='icon20' on the double? overload - the originals
            // really do differ, and transcription keeps the difference.
            return new MvcHtmlString(string.Format(
                "<span style='color:#00FFFF' nicetitle='Metal available to you/owned by faction'><img src='{0}' width='20' height='20'/>{1} / {2}</span>",
                GlobalConst.MetalIcon, Math.Floor(account.GetMetalAvailable()), Math.Floor(account.Faction.Metal)));
        }

        public static MvcHtmlString PrintBombers(this IHtmlHelper helper, double? count)
            // no Math.Floor on this one, unlike its siblings
            => new MvcHtmlString(string.Format("<span>{0}<img src='{1}' class='icon20'/></span>",
                count ?? 0, GlobalConst.BomberIcon));

        public static MvcHtmlString PrintBombers(this IHtmlHelper helper, Account account)
        {
            if (account == null || account.Faction == null) return null;
            return new MvcHtmlString(string.Format(
                "<span nicetitle='Bombers available to you/owned by faction'><img src='{0}' class='icon20'/>{1} / {2}</span>",
                GlobalConst.BomberIcon, Math.Floor(account.GetBombersAvailable()), Math.Floor(account.Faction.Bombers)));
        }

        public static MvcHtmlString PrintWarps(this IHtmlHelper helper, double? count)
            // no Math.Floor, same as PrintBombers and unlike PrintEnergy
            => new MvcHtmlString(string.Format("<span>{0}<img src='{1}' class='icon20'/></span>",
                count ?? 0, GlobalConst.WarpIcon));

        public static MvcHtmlString PrintWarps(this IHtmlHelper helper, Account account)
        {
            if (account == null || account.Faction == null) return null;
            return new MvcHtmlString(string.Format(
                "<span nicetitle='Warp cores available to you/owned by faction'><img src='{0}' class='icon20'/>{1} / {2}</span>",
                GlobalConst.WarpIcon, Math.Floor(account.GetWarpAvailable()), Math.Floor(account.Faction.Warps)));
        }

        /// <summary>
        /// <c>helper.Encode(text).Replace("\n", "&lt;br/&gt;")</c>, with the encoder named
        /// rather than inherited - and that is the whole point.
        ///
        /// MVC 5's <c>HtmlHelper.Encode</c> is <c>HttpUtility.HtmlEncode</c>. ASP.NET Core's
        /// <c>IHtmlHelper.Encode</c> is not the same function: it escapes a newline to
        /// <c>&amp;#xA;</c>, so the Replace matches nothing and every line break disappears -
        /// silently, in forum posts and descriptions. It also escapes non-ASCII, so a player
        /// called "Müller" would come out as "M&amp;#xFC;ller".
        ///
        /// <c>WebUtility.HtmlEncode</c> is HttpUtility.HtmlEncode's actual counterpart on
        /// .NET 9 - same five characters, nothing else - so naming it restores the original
        /// byte for byte and lets the method keep its original shape.
        /// </summary>
        public static MvcHtmlString PrintLines(this IHtmlHelper helper, string text)
            => new MvcHtmlString(System.Net.WebUtility.HtmlEncode(text).Replace("\n", "<br/>"));


        /// <summary>
        /// The link helpers need URL generation, which the formatters above do not.
        ///
        /// MVC 5 reached it through <c>Global.UrlHelper()</c> - another ambient static. Here
        /// it comes from the helper's own ViewContext, which is where ASP.NET Core keeps it,
        /// so these need no ambient at all.
        /// </summary>
        private static IUrlHelper Url(IHtmlHelper helper)
        {
            // A null helper is not a mistake here. Several of these are called from CONTROLLERS
            // rather than views - AutocompleteController passes null to PrintMap and PrintClan -
            // and on MVC 5 that was harmless, because the originals ignore the helper entirely and
            // use Global.UrlHelper(). The ported ones went through the argument instead, so the
            // same call threw NullReferenceException and every map autocomplete answered 500.
            // Nothing noticed, because no request had ever been made to one of those URLs.
            if (helper == null) return ZeroKWeb.Global.UrlHelper();

            var context = helper.ViewContext;
            var factory = context.HttpContext.RequestServices.GetRequiredService<IUrlHelperFactory>();
            return factory.GetUrlHelper(context);
        }

        public static MvcHtmlString PrintDropships(this IHtmlHelper helper, double? count, Faction faction)
            => new MvcHtmlString(string.Format("<span>{0}<img src='{1}' class='icon20'/></span>",
                Math.Floor(count ?? 0), faction.GetShipImageUrl()));

        public static MvcHtmlString PrintDropships(this IHtmlHelper helper, Account account)
        {
            if (account == null || account.Faction == null) return null;
            return new MvcHtmlString(string.Format(
                "<span nicetitle='Dropships available to you/owned by faction'><img src='{0}' class='icon20'/>{1} / {2}</span>",
                account.Faction.GetShipImageUrl(), Math.Floor(account.GetDropshipsAvailable()),
                Math.Floor(account.Faction.Dropships)));
        }

        /// <summary>Delegates to the one implementation in ZkData/ZkHtmlFormat.cs.</summary>
        public static MvcHtmlString PrintFaction(this IHtmlHelper helper, Faction fac, bool big = true)
            => new MvcHtmlString(ZkHtmlFormat.PrintFaction(WebContext(helper), fac, big));

        /// <summary>Delegates to the one implementation in ZkData/ZkHtmlFormat.cs.</summary>
        public static MvcHtmlString PrintClan(this IHtmlHelper helper, Clan clan, bool colorize = true, bool big = false)
            => new MvcHtmlString(ZkHtmlFormat.PrintClan(WebContext(helper), clan, colorize, big));

        public static MvcHtmlString PrintLines(this IHtmlHelper helper, IEnumerable<object> lines)
        {
            var sb = new StringBuilder();
            foreach (var line in lines) sb.AppendFormat("{0}<br/>", line);
            return new MvcHtmlString(sb.ToString());
        }
    }
}
