using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LobbyClient;
using PlasmaShared;

namespace ZkData
{
    /// <summary>
    /// Building a PlanetWars event, as one implementation both processes can reach.
    ///
    /// **This was the last piece of Phase 1 coupling, and it ran the other way.** Everything else
    /// was the website reaching into the lobby server; this was the lobby server reaching into the
    /// WEBSITE - `IPlanetwarsEventCreator` was implemented once, in Zero-K.info, and it formats the
    /// event feed's HTML. The server calls it from 19 places, so a lobby server in its own process
    /// could not write an event at all.
    ///
    /// Two things were needed and are now parameters rather than ambients:
    ///
    /// - <see cref="ZkHtmlContext"/> - who is looking and how to build a URL. The website passes
    ///   its per-request one, so its events are formatted exactly as before; a standalone server
    ///   passes one built from GlobalConst.BaseSiteUrl, because it has no request.
    /// - <paramref name="say"/> - how to tell a clan or faction channel about the event. In the
    ///   website that was Global.LobbyApi.GhostSay; in the server it is the server itself.
    ///
    /// The body was moved by extraction, not retyped. What it does is unchanged, including the
    /// part that is arguably wrong: an event's HTML is rendered once, at creation, so a colour
    /// that depends on the viewer is baked in for everyone who reads the feed afterwards. That is
    /// today's behaviour and this is not the change that should alter it.
    /// </summary>
    public static class PlanetwarsEventFormatter
    {
        /// <summary>
        /// <paramref name="say"/> may be null, which is what the website did when no lobby server
        /// was attached - the event is still built, the channels are simply not told.
        /// </summary>
        public static Event CreateEvent(ZkHtmlContext ctx, Action<Say> say, string format, params object[] args)
        {

        var ev = new Event() { Time = DateTime.UtcNow };

        ev.PlainText = String.Format(format, args);
        var orgArgs = new List<object>(args);
        var alreadyAddedEvents = new List<object>();

        for (var i = 0; i < args.Length; i++) {
            var dontDuplicate = false; // set to true for args that have their own Event table in DB, e.g. accounts, factions, clans, planets
            var arg = args[i];
            if (arg == null) continue;
                        var eventAlreadyExists = alreadyAddedEvents.Contains(arg);

            if (arg is Account) {
                var acc = (Account)arg;
                args[i] = ZkHtmlFormat.PrintAccount(ctx, acc);
                if (!eventAlreadyExists) {
                    if (!ev.Accounts.Any(x => x.AccountID == acc.AccountID)) ev.Accounts.Add(acc);
                    dontDuplicate = true;
                }
            } else if (arg is Clan) {
                var clan = (Clan)arg;
                args[i] = ZkHtmlFormat.PrintClan(ctx, clan);
                if (!eventAlreadyExists) {
                    ev.Clans.Add(clan);
                    dontDuplicate = true;
                }
            } else if (arg is Planet) {
                var planet = (Planet)arg;
                args[i] = ZkHtmlFormat.PrintPlanet(ctx, planet);
                if (!eventAlreadyExists) {
                    if (planet.PlanetID != 0) ev.Planets.Add(planet);
                    dontDuplicate = true;
                }
            } else if (arg is SpringBattle) {
                var bat = (SpringBattle)arg;
                args[i] = String.Format("<a href='{0}'>B{1}</a>", ctx.Action("Detail", "Battles", new { id = bat.SpringBattleID }),
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
                args[i] = ZkHtmlFormat.PrintFaction(ctx, fac, false);
                if (!eventAlreadyExists) {
                    ev.Factions.Add(fac);
                    dontDuplicate = true;
                }
            } else if (arg is StructureType) {
                var stype = (StructureType)arg;
                args[i] = ZkHtmlFormat.PrintStructureType(ctx, stype);
            } else if (arg is FactionTreaty) {
                var tr = (FactionTreaty)arg;
                args[i] = ZkHtmlFormat.PrintFactionTreaty(ctx, tr);
            } else if (arg is RoleType) {
                var rt = (RoleType)arg;
                args[i] = ZkHtmlFormat.PrintRoleType(ctx, rt);
            } else {
                // Anything that is not an entity stayed as the caller passed it, and ev.Text is
                // rendered with @Html.Raw on Planetwars/Events.cshtml - so a plain string went
                // into the page as markup. The one that matters is a planet's new name:
                // SubmitRenamePlanet validates it for whitespace and nothing else, and passes it
                // here as a string.
                //
                // Safe to encode because no caller passes HTML: all 60 CreateEvent call sites
                // pass entities, numbers or plain text, which is the property this relies on.
                // The entity branches above produce markup deliberately and are left alone, and
                // ev.PlainText was built before this loop, so the lobby channels are unaffected.
                args[i] = System.Net.WebUtility.HtmlEncode(arg.ToString());
            }

            if (dontDuplicate) alreadyAddedEvents.Add(arg);
        }

        ev.Text = String.Format(format, args);
        try {
            if (say != null) {
                foreach (var clan in orgArgs.OfType<Clan>().Where(x => x != null)) say(
                    new Say() { User = GlobalConst.NightwatchName, IsEmote = true, Place = SayPlace.Channel,Target = clan.GetClanChannel(), Text = ev.PlainText});
                foreach (var faction in orgArgs.OfType<Faction>().Where(x => x != null))
                    say(
                        new Say() { User = GlobalConst.NightwatchName, IsEmote = true, Place = SayPlace.Channel, Target = faction.Shortcut, Text = ev.PlainText });
            }
        } catch (Exception ex) {
            Trace.TraceError("Error sending event to channels: {0}", ex);
        }

        return ev;
        }
    }
}
