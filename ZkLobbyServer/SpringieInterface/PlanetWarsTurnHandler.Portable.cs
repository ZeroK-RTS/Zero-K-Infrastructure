using System;
using System.Linq;
using ZeroKWeb;
using ZkData;

/// <summary>
/// The half of PlanetWarsTurnHandler the WEBSITE calls, split off so it can be linked into
/// the .NET 9 port.
///
/// Ten call sites across four controllers - Planetwars, Clans, Factions - call SetPlanetOwners
/// directly. That is a static call from Zero-K.info into the ZkLobbyServer ASSEMBLY, and it is
/// coupling ILobbyServerApi never modelled: that seam covers what goes through Global.LobbyApi,
/// and this never did.
///
/// It turned out not to be coupling to the RUNNING server either. SetPlanetOwners, WinGame and
/// ReturnPeacefulDropshipsHome take a ZkDataContext and an IPlanetwarsEventCreator and touch
/// nothing else; they are database logic that happens to live in the lobby server's project.
/// The methods that do need the server - EndTurn, ProcessBattleResult, ProcessGalaxyTick, which
/// take a ZkLobbyServer parameter and reach PlanetWarsMatchMaker - stay next door, and the
/// website calls none of them.
///
/// So this is a relocation, not a rewrite, and `partial` keeps the two halves one class for the
/// Framework build. Properly these three belong in ZkData; that is a larger move than this
/// change, and it is what should eventually happen to them.
/// </summary>
public static partial class PlanetWarsTurnHandler
{
    public static void SetPlanetOwners(IPlanetwarsEventCreator eventCreator, ZkDataContext db = null, SpringBattle sb = null)
    {
        if (db == null) db = new ZkDataContext();

        Galaxy gal = db.Galaxies.Single(x => x.IsDefault);
        foreach (Planet planet in gal.Planets)
        {
            if (planet.OwnerAccountID != null) foreach (var ps in planet.PlanetStructures.Where(x => x.OwnerAccountID == null))
                {
                    ps.OwnerAccountID = planet.OwnerAccountID;
                    ps.ReactivateAfterBuild();
                }


            PlanetFaction best = planet.PlanetFactions.OrderByDescending(x => x.Influence).FirstOrDefault();
            Faction newFaction = planet.Faction;
            Account newAccount = planet.Account;

            if (best == null || best.Influence < GlobalConst.InfluenceToCapturePlanet)
            {
                // planet not capture

                if (planet.Faction != null)
                {
                    var curFacInfluence =
                        planet.PlanetFactions.Where(x => x.FactionID == planet.OwnerFactionID).Select(x => x.Influence).FirstOrDefault();

                    if (curFacInfluence <= GlobalConst.InfluenceToLosePlanet)
                    {
                        // owners have too small influence, planet belong to nobody
                        newFaction = null;
                        newAccount = null;
                    }
                }
            }
            else
            {
                if (best.Faction != planet.Faction)
                {
                    newFaction = best.Faction;

                    // best attacker without planets
                    Account candidate =
                        planet.AccountPlanets.Where(
                            x => x.Account.FactionID == newFaction.FactionID && x.AttackPoints > 0 && !x.Account.Planets.Any()).OrderByDescending(
                                x => x.AttackPoints).Select(x => x.Account).FirstOrDefault();

                    if (candidate == null)
                    {
                        // best attacker
                        candidate =
                            planet.AccountPlanets.Where(x => x.Account.FactionID == newFaction.FactionID && x.AttackPoints > 0).OrderByDescending(
                                x => x.AttackPoints).ThenBy(x => x.Account.Planets.Count()).Select(x => x.Account).FirstOrDefault();
                    }

                    // best player without planets
                    if (candidate == null)
                    {
                        candidate =
                            newFaction.Accounts.Where(x => !x.Planets.Any()).OrderByDescending(x => x.AccountPlanets.Sum(y => y.AttackPoints)).
                                FirstOrDefault();
                    }

                    // best with planets
                    if (candidate == null)
                    {
                        candidate =
                            newFaction.Accounts.OrderByDescending(x => x.AccountPlanets.Sum(y => y.AttackPoints)).
                                FirstOrDefault();
                    }

                    newAccount = candidate;
                }
            }

            // change has occured
            if (newFaction != planet.Faction)
            {
                // disable structures
                foreach (PlanetStructure structure in planet.PlanetStructures.Where(x => x.StructureType.OwnerChangeDisablesThis))
                {
                    structure.ReactivateAfterBuild();
                    structure.Account = newAccount;
                }

                // delete structures being lost on planet change
                foreach (PlanetStructure structure in
                    planet.PlanetStructures.Where(structure => structure.StructureType.OwnerChangeDeletesThis).ToList()) db.PlanetStructures.DeleteOnSubmit(structure);

                // reset attack points memory
                foreach (AccountPlanet acp in planet.AccountPlanets) acp.AttackPoints = 0;

                if (newFaction == null)
                {
                    Account account = planet.Account;
                    Clan clan = null;
                    if (account != null)
                    {
                        clan = planet.Account != null ? planet.Account.Clan : null;
                    }

                    db.Events.InsertOnSubmit(eventCreator.CreateEvent("{0} planet {1} owned by {2} {3} was abandoned. {4}",
                                                                planet.Faction,
                                                                planet,
                                                                account,
                                                                clan,
                                                                sb));
                    if (account != null)
                    {
                        eventCreator.GhostPm(planet.Account.Name, string.Format(
                            "Warning, you just lost planet {0}!! {2}/PlanetWars/Planet/{1}",
                            planet.Name,
                            planet.PlanetID,
                            GlobalConst.BaseSiteUrl));
                    }
                }
                else
                {
                    // new real owner

                    // log messages
                    if (planet.OwnerAccountID == null) // no previous owner
                    {
                        db.Events.InsertOnSubmit(eventCreator.CreateEvent("{0} has claimed planet {1} for {2} {3}. {4}",
                                                                    newAccount,
                                                                    planet,
                                                                    newFaction,
                                                                    newAccount.Clan,
                                                                    sb));
                        eventCreator.GhostPm(newAccount.Name, string.Format(
                            "Congratulations, you now own planet {0}!! {2}/PlanetWars/Planet/{1}",
                            planet.Name,
                            planet.PlanetID,
                            GlobalConst.BaseSiteUrl));
                    }
                    else
                    {
                        db.Events.InsertOnSubmit(eventCreator.CreateEvent("{0} of {1} {2} has captured planet {3} from {4} of {5} {6}. {7}",
                                                                    newAccount,
                                                                    newFaction,
                                                                    newAccount.Clan,
                                                                    planet,
                                                                    planet.Account,
                                                                    planet.Faction,
                                                                    planet.Account.Clan,
                                                                    sb));

                        eventCreator.GhostPm(newAccount.Name, string.Format(
                            "Congratulations, you now own planet {0}!! {2}/PlanetWars/Planet/{1}",
                            planet.Name,
                            planet.PlanetID,
                            GlobalConst.BaseSiteUrl));

                        eventCreator.GhostPm(planet.Account.Name, string.Format(
                            "Warning, you just lost planet {0}!! {2}/PlanetWars/Planet/{1}",
                            planet.Name,
                            planet.PlanetID,
                            GlobalConst.BaseSiteUrl));
                    }


                    if (planet.PlanetStructures.Any(x => x.StructureType.OwnerChangeWinsGame))
                    {
                        WinGame(db, gal, newFaction, eventCreator.CreateEvent("CONGRATULATIONS!! {0} has won the PlanetWars by capturing {1} planet {2}!", newFaction, planet.Faction, planet));
                    }
                }

                planet.Faction = newFaction;
                planet.Account = newAccount;
            }
            ReturnPeacefulDropshipsHome(db, planet);
        }
        db.SaveChanges();
    }


    public static void WinGame(ZkDataContext db,Galaxy gal, Faction winnerFaction, Event ev)
    {
        MiscVar.PlanetWarsMode = PlanetWarsModes.AllOffline;
        gal.Ended = DateTime.UtcNow;
        gal.EndMessage = ev.PlainText;
        gal.WinnerFaction = winnerFaction;
        MiscVar.PlanetWarsMode = PlanetWarsModes.AllOffline;
        db.Events.Add(ev);
    }


    public static void ReturnPeacefulDropshipsHome(ZkDataContext db, Planet planet)
    {
        //    return dropshuips home if owner is ceasefired/allied/same faction
        if (planet.Faction != null)
        {
            foreach (PlanetFaction entry in planet.PlanetFactions.Where(x => x.Dropships > 0))
            {
                if (entry.FactionID == planet.OwnerFactionID ||
                    planet.Faction.HasTreatyRight(entry.Faction, x => x.EffectPreventDropshipAttack == true, planet))
                {
                    planet.Faction.SpendDropships(-entry.Dropships);
                    entry.Dropships = 0;
                }
            }
        }
    }
}
