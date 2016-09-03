using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using JetBrains.Annotations;
using LobbyClient;
using ZeroKWeb;
using ZkData;


public class PlanetwarsEventCreator:IPlanetwarsEventCreator {
    /// <summary>
    /// Converts a given string and its arguments into an Event
    /// </summary>
    /// <param name="format">String to format; converted objects are inserted into the string - e.g. "Planet {0} captured by {1}"</param>
    /// <param name="args">Objects can be DB objects of various types; e.g. <see cref="Account"/>, <see cref="Clan"/>, <see cref="Planet"/></param>
    [StringFormatMethod("format")]
    public static Event CreateEvent(string format, params object[] args)
    {
        var ev = new Event() { Time = DateTime.UtcNow };

        ev.PlainText = String.Format(format, args);
        var orgArgs = new List<object>(args);
        var alreadyAddedEvents = new List<object>();

        for (var i = 0; i < args.Length; i++) {
            var dontDuplicate = false; // set to true for args that have their own Event table in DB, e.g. accounts, factions, clans, planets
            var arg = args[i];
            if (arg == null) continue;
            var url = Global.UrlHelper();
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
                args[i] = String.Format("<a href='{0}'>B{1}</a>", url.Action("Detail", "Battles", new { id = bat.SpringBattleID }),
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

        ev.Text = String.Format(format, args);
        try {
            if (Global.Server != null) {
                foreach (var clan in orgArgs.OfType<Clan>().Where(x => x != null)) Global.Server.GhostSay(
                    new Say() { User = GlobalConst.NightwatchName, IsEmote = true, Place = SayPlace.Channel,Target = clan.GetClanChannel(), Text = ev.PlainText});
                foreach (var faction in orgArgs.OfType<Faction>().Where(x => x != null))
                    Global.Server.GhostSay(
                        new Say() { User = GlobalConst.NightwatchName, IsEmote = true, Place = SayPlace.Channel, Target = faction.Shortcut, Text = ev.PlainText });
            }
        } catch (Exception ex) {
            Trace.TraceError("Error sending event to channels: {0}", ex);
        }

        return ev;
    }

    Event IPlanetwarsEventCreator.CreateEvent(string format, params object[] args)
    {
        return CreateEvent(format, args);
    }

    public void GhostPm(string user, string text)
    {
        Global.Server.GhostPm(user, text);
    }

}