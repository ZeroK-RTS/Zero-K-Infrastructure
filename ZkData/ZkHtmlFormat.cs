using System;
using System.Collections.Generic;
using System.Linq;
using PlasmaShared;

namespace ZkData
{
    /// <summary>
    /// Who is looking, and how to make a link. Everything the entity formatters below used to
    /// read off the website's per-request ambient state.
    ///
    /// The lobby server has no request and no viewer, which is why this is a parameter rather
    /// than a static: it is the difference between formatting that can move out of the website
    /// and formatting that cannot.
    /// </summary>
    public class ZkHtmlContext
    {
        /// <summary>action, controller, route values -&gt; url. Empty string when nothing can build one.</summary>
        public Func<string, string, object, string> Action = (action, controller, values) => "";

        public int ViewerFactionID;
        public int? ViewerClanID;
        public bool ViewerIsModerator;
    }

    /// <summary>
    /// The entity formatters - account, clan, faction, planet, structure type, treaty, role -
    /// as ONE implementation.
    ///
    /// There were two, near-verbatim copies of each other: MVC 5's
    /// Zero-K.info/AppCode/HtmlHelperExtensions.cs and the port's
    /// ZeroKWeb.Core/Mvc5Compat/HtmlHelperExtensions.Ported.cs. Their format strings were
    /// identical, which was checked before they were merged rather than assumed. Both now
    /// delegate here, and so does the PlanetWars event feed - which is the reason this moved:
    /// the lobby server writes events, and it cannot reach into the website to format them.
    ///
    /// **The bodies were moved by extraction, not retyped.** The three ambients became context
    /// fields and the MvcHtmlString wrapper came off; nothing else was touched. Nine recorded
    /// outputs in ZeroKWeb.Render pin the result - see CheckPortedHelpers, which records what
    /// the implementation produced rather than what anyone believed it produced.
    ///
    /// **URL generation is passed through, not reimplemented.** Hard-coding "/Clans/Detail/{id}"
    /// would have been tidier and would have changed what every existing caller emits, which the
    /// render harness cannot check because it has no route table. So each caller supplies its own
    /// Action and nothing about existing URLs moves.
    /// </summary>
    public static class ZkHtmlFormat
    {

        public static string PrintAccount(ZkHtmlContext ctx, Account account,
            bool colorize = true, bool ignoreDeleted = false, bool makeLinks = true)
        {
            if (account == null) return "Nobody";
            if (account.IsDeleted && !ignoreDeleted && !ctx.ViewerIsModerator) return account.Name;

            var clanStr = "";
            if (account.Clan != null)
            {
                clanStr = string.Format("<img src='{0}' width='16'/>", account.Clan.GetImageUrl());
                if (makeLinks)
                    clanStr = string.Format("<a href='{1}' nicetitle='$clan${2}'>{0}</a>",
                        clanStr, ctx.Action("Detail", "Clans", new { id = account.ClanID }), account.ClanID);
            }
            else if (account.Faction != null)
                clanStr = string.Format("<img src='{0}' width='16'/>", account.Faction.GetImageUrl());

            var dudeStr = "";
            if (account.AdminLevel >= AdminLevel.Moderator)
                dudeStr = "<img src='/img/police.png'  class='icon16' alt='Admin' />";

            var color = Faction.FactionColor(account.Faction, ctx.ViewerFactionID);
            if (string.IsNullOrEmpty(color)) color = "#B0D0C0";

            var flag = string.Format("<img src='/img/flags/{0}.png' class='flag' height='11' width='16' alt='{0}'/>",
                (account.Country != "??" && !account.HideCountry) ? account.Country : "unknown");
            var rank = string.Format("<img src='/img/ranks/{0}.png'  class='icon16' alt='rank' />",
                account.GetIconName());

            var name = account.Name;
            if (account.IsDeleted) name += "(REDACTED)";
            var user = name;
            if (makeLinks)
                user = string.Format("<a href='/Users/Detail/{0}' style='color:{1}' nicetitle='$user${0}'>{2}</a>",
                    account.AccountID, colorize ? color : "", name);

            return string.Format("{0}{1}{2}{3}{4}", flag, rank, clanStr, dudeStr, user);
        }


        public static string PrintClan(ZkHtmlContext ctx, Clan clan, bool colorize = true, bool big = false)
        {
            if (clan == null)
                return string.Format("<a href='{0}'>No Clan</a>", ctx.Action("Index", "Clans", null));

            var color = Clan.ClanColor(clan, ctx.ViewerClanID);
            if (string.IsNullOrEmpty(color)) color = "#B0D0C0";

            if (big)
                return string.Format("<a href='{1}' nicetitle='$clan${2}'><img width='64' src='{0}'/></a>",
                    clan.GetImageUrl(), ctx.Action("Detail", "Clans", new { id = clan.ClanID }), clan.ClanID);

            return string.Format(
                "<a href='{0}' nicetitle='$clan${4}'><img src='{1}' width='16'><span style='color:{2}'>{3}</span></a>",
                ctx.Action("Detail", "Clans", new { id = clan.ClanID }), clan.GetImageUrl(),
                colorize ? color : "", System.Net.WebUtility.HtmlEncode(clan.Shortcut), clan.ClanID);
        }


        // Encoded from here down, as PrintClan already did with a clan's shortcut. The
        // inconsistency ran the wrong way: the shortcut is charset-validated at creation and WAS
        // encoded, while a planet's name is free text - SubmitRenamePlanet checks it for
        // whitespace and nothing else - and was not. All of this output ends up in Event.Text,
        // which Planetwars/Events.cshtml renders with @Html.Raw, so it is markup by the time a
        // visitor sees it.
        //
        // These are privileged-user texts rather than anybody's, which is why this is defence in
        // depth and not an incident: it stops a moderator account, or whoever has taken one,
        // turning a planet rename into script that runs for every visitor to that page. Encoding
        // a normal name, colour or icon filename returns it unchanged.
        public static string PrintFaction(ZkHtmlContext ctx, Faction fac, bool big = true)
        {
            if (fac == null) return "";
            if (big)
                return string.Format("<a href='{1}' nicetitle='$faction${2}'><img src='{0}'/></a>",
                    fac.GetImageUrl(), ctx.Action("Detail", "Factions", new { id = fac.FactionID }), fac.FactionID);

            // two spaces before style= in the original, kept
            return string.Format(
                "<a href='{3}' nicetitle='$faction${4}'><span style='color:{0}'><img src='{1}'  style='width:16px;height:16px'/>{2}</span></a>",
                System.Net.WebUtility.HtmlEncode(fac.Color), fac.GetImageUrl(), System.Net.WebUtility.HtmlEncode(fac.Shortcut),
                ctx.Action("Detail", "Factions", new { id = fac.FactionID }), fac.FactionID);
        }


        public static string PrintPlanet(ZkHtmlContext ctx, Planet planet)
        {
            if (planet == null) return "?";
            return string.Format(
                "<a href='{0}' title='$planet${4}' style='{5}'><img src='/img/planets/{1}' width='{2}'>{3}</a>",
                ctx.Action("Planet", "Planetwars", new { id = planet.PlanetID }),
                System.Net.WebUtility.HtmlEncode(planet.Resource.MapPlanetWarsIcon),
                planet.Resource.PlanetWarsIconSize / 3,
                System.Net.WebUtility.HtmlEncode(planet.Name),
                planet.PlanetID,
                planet.Faction != null ? "color:" + System.Net.WebUtility.HtmlEncode(planet.Faction.Color) : "");
        }


        public static string PrintStructureType(ZkHtmlContext ctx, StructureType stype)
        {
            // the original calls the url helper here and never uses it; not carried over,
            // because carrying it would mean calling into request state for nothing
            if (stype == null) return "";
            return string.Format("<span nicetitle='$structuretype${0}'>{1}</span>",
                stype.StructureTypeID, System.Net.WebUtility.HtmlEncode(stype.Name));
        }


        public static string PrintFactionTreaty(ZkHtmlContext ctx, FactionTreaty treaty)
        {
            if (treaty == null) return "";
            // the original's markup really does close a </span> it never opened
            return string.Format("<a href='{1}' nicetitle='$treaty${0}'>TR{0}</span></a>",
                treaty.FactionTreatyID,
                ctx.Action("TreatyDetail", "Factions", new { id = treaty.FactionTreatyID }));
        }


        public static string PrintRoleType(ZkHtmlContext ctx, RoleType rt)
        {
            var factoids = new List<string>();
            if (rt.IsClanOnly) factoids.Add("clan based");
            if (rt.IsOnePersonOnly) factoids.Add("only one person can hold this");
            if (rt.IsVoteable) factoids.Add("is voteable");
            if (rt.RoleTypeHierarchiesByMasterRoleTypeID.Any(x => x.CanAppoint))
                factoids.Add("appoints: " + string.Join(", ",
                    rt.RoleTypeHierarchiesByMasterRoleTypeID.Where(x => x.CanAppoint).Select(x => x.SlaveRoleType.Name)));
            if (rt.RoleTypeHierarchiesByMasterRoleTypeID.Any(x => x.CanRecall))
                factoids.Add("recalls: " + string.Join(", ",
                    rt.RoleTypeHierarchiesByMasterRoleTypeID.Where(x => x.CanRecall).Select(x => x.SlaveRoleType.Name)));
            if (rt.RightBomberQuota != 0) factoids.Add(string.Format("bomber quota {0:F0}%", rt.RightBomberQuota * 100));
            if (rt.RightDropshipQuota != 0) factoids.Add(string.Format("dropship quota {0:F0}%", rt.RightDropshipQuota * 100));
            if (rt.RightWarpQuota != 0) factoids.Add(string.Format("warp quota {0:F0}%", rt.RightWarpQuota * 100));
            if (rt.RightMetalQuota != 0) factoids.Add(string.Format("metal quota {0:F0}%", rt.RightMetalQuota * 100));
            if (rt.RightSetEnergyPriority) factoids.Add("can set energy priorities");
            if (rt.RightDiplomacy) factoids.Add("can control diplomacy");
            if (rt.RightEditTexts) factoids.Add("controls texts");

            // &nbsp without the semicolon, as found
            return string.Format("<span title=\"<b>{0}</b><ul>{1}</ul>\"><b>{2}</b></span>",
                System.Net.WebUtility.HtmlEncode(rt.Description),
                string.Join("", factoids.Select(x => "<li>" + System.Net.WebUtility.HtmlEncode(x) + "</li>")),
                System.Net.WebUtility.HtmlEncode(rt.Name) + "&nbsp");
        }
    }
}
