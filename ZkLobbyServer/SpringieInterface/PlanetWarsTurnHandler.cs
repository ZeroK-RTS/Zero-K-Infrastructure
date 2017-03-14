using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using LobbyClient;
using ZeroKWeb;
using ZeroKWeb.SpringieInterface;
using ZkData;

public static class PlanetWarsTurnHandler
{

    /// <summary>
    /// Process planet wars turn
    /// </summary>
    /// <param name="mapName"></param>
    /// <param name="extraData"></param>
    /// <param name="db"></param>
    /// <param name="winNum">0 = attacker wins, 1 = defender wins</param>
    /// <param name="players"></param>
    /// <param name="text"></param>
    /// <param name="sb"></param>
    public static void EndTurn(string mapName, List<string> extraData, ZkDataContext db, int? winNum, List<Account> players, StringBuilder text, SpringBattle sb, List<Account> attackers, IPlanetwarsEventCreator eventCreator)
    {
        if (extraData == null) extraData = new List<string>();
        Galaxy gal = db.Galaxies.Single(x => x.IsDefault);
        Planet planet = gal.Planets.Single(x => x.Resource.InternalName == mapName);

        text.AppendFormat("Battle on {1}/PlanetWars/Planet/{0} has ended\n", planet.PlanetID, GlobalConst.BaseSiteUrl);


        Faction attacker = attackers.Where(x => x.Faction != null).Select(x => x.Faction).First();
        var defenders = players.Where(x => x.FactionID != attacker.FactionID && x.FactionID != null).ToList();
        bool isAttackerWinner;
        bool wasAttackerCcDestroyed = false;
        bool wasDefenderCcDestroyed = false;

        if (winNum != null)
        {
            if (winNum == 0) isAttackerWinner = true;
            else isAttackerWinner = false;

            wasAttackerCcDestroyed = extraData.Any(x => x.StartsWith("hqkilled,0"));
            wasDefenderCcDestroyed = extraData.Any(x => x.StartsWith("hqkilled,1"));
        }
        else
        {
            text.AppendFormat("No winner on this battle!");
            Trace.TraceError("PW battle without a winner {0}", sb != null ? sb.SpringBattleID : (int?)null);
            return;
        }

        int dropshipsSent = (planet.PlanetFactions.Where(x => x.Faction == attacker).Sum(x => (int?)x.Dropships) ?? 0);
        bool isLinked = planet.CanDropshipsAttack(attacker);
        string influenceReport = "";

        // distribute influence
        // save influence gains
        // give influence to main attackers
        double planetDropshipDefs = (planet.PlanetStructures.Where(x => x.IsActive).Sum(x => x.StructureType.EffectDropshipDefense) ?? 0);
        double planetIpDefs = (planet.PlanetStructures.Where(x => x.IsActive).Sum(x => x.StructureType.EffectReduceBattleInfluenceGain) ?? 0);

        double baseInfluence = GlobalConst.BaseInfluencePerBattle;
        double influence = baseInfluence;


        double effectiveShips = Math.Max(0, (dropshipsSent - planetDropshipDefs));
        double shipBonus = effectiveShips*GlobalConst.InfluencePerShip;

        double defenseBonus = -planetIpDefs;
        double techBonus = attacker.GetFactionUnlocks().Count() * GlobalConst.InfluencePerTech;
        double ipMultiplier = 1;
        string ipReason;
        if (!isAttackerWinner)
        {
            if (wasDefenderCcDestroyed)
            {
                ipMultiplier = 0.2;
                ipReason = "from losing but killing defender's CC";
            }
            else
            {
                ipMultiplier = 0;
                ipReason = "from losing horribly";
            }
        }
        else
        {
            if (wasAttackerCcDestroyed)
            {
                ipReason = "from losing own CC";
                ipMultiplier = 0.5;
            }
            else
            {
                ipReason = "from winning flawlessly";
            }
        }
        if (!isLinked && effectiveShips < GlobalConst.DropshipsForFullWarpIPGain)
        {
            var newMult = effectiveShips/GlobalConst.DropshipsForFullWarpIPGain;
            ipMultiplier *= newMult ;
            ipReason = ipReason + string.Format(" and reduced to {0}% because only {1} of {2} ships needed for warp attack got past defenses", (int)(newMult*100.0), (int)effectiveShips, GlobalConst.DropshipsForFullWarpIPGain);
        }


        influence = (influence + shipBonus + techBonus) * ipMultiplier + defenseBonus;
        if (influence < 0) influence = 0;
        influence = Math.Floor(influence * 100) / 100;

        // main winner influence 
        PlanetFaction entry = planet.PlanetFactions.FirstOrDefault(x => x.Faction == attacker);
        if (entry == null)
        {
            entry = new PlanetFaction { Faction = attacker, Planet = planet, };
            planet.PlanetFactions.Add(entry);
        }

        entry.Influence += influence;


        // clamping of influence
        // gained over 100, sole owner
        if (entry.Influence >= 100)
        {
            entry.Influence = 100;
            foreach (var pf in planet.PlanetFactions.Where(x => x.Faction != attacker)) pf.Influence = 0;
        }
        else
        {
            var sumOthers = planet.PlanetFactions.Where(x => x.Faction != attacker).Sum(x => (double?)x.Influence) ?? 0;
            if (sumOthers + entry.Influence > 100)
            {
                var excess = sumOthers + entry.Influence - 100;
                foreach (var pf in planet.PlanetFactions.Where(x => x.Faction != attacker)) pf.Influence -= pf.Influence / sumOthers * excess;
            }
        }
        try
        {
            influenceReport = String.Format("{0} gained {1} influence ({2}% {3} of {4}{5}{6}{7})",
                attacker.Shortcut,
                influence,
                (int)(ipMultiplier * 100.0),
                ipReason,
                baseInfluence + " base",
                techBonus > 0 ? " +" + techBonus + " from techs" : "",
                shipBonus > 0 ? " +" + shipBonus + " from dropships" : "",
                defenseBonus != 0 ? " -" + -defenseBonus + " from defenses" : "");
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
        }


        // distribute metal
        var attackersTotalMetal = Math.Floor(GlobalConst.PlanetWarsAttackerMetal);
        var attackerMetal = Math.Floor(attackersTotalMetal / attackers.Count);
        foreach (Account w in attackers)
        {
            w.ProduceMetal(attackerMetal);
            var ev = eventCreator.CreateEvent("{0} gained {1} metal from battle {2}",
                                        w,
                                        attackerMetal,
                                        sb);
            db.Events.InsertOnSubmit(ev);
            text.AppendLine(ev.PlainText);
        }


        var defendersTotalMetal = Math.Floor(GlobalConst.PlanetWarsDefenderMetal);
        if (defenders.Count > 0)
        {
            var defenderMetal = Math.Floor(defendersTotalMetal/defenders.Count);
            foreach (Account w in defenders)
            {

                w.ProduceMetal(defenderMetal);
                var ev = eventCreator.CreateEvent("{0} gained {1} metal from battle {2}", w, defenderMetal, sb);

                db.Events.InsertOnSubmit(ev);
                text.AppendLine(ev.PlainText);
            }
        }
        else
        {
            // planet had no defenders, give metal to owner's faction
            if (planet.OwnerFactionID !=null) planet.Faction.ProduceMetal(defendersTotalMetal);
        }


        // remove attacker's dropships
        foreach (var pf in planet.PlanetFactions.Where(x => x.Faction == attacker)) pf.Dropships = 0;


        // remove dropships staying for too long (return to faction pool)
        foreach (var pf in planet.PlanetFactions.Where(x => x.Faction != attacker && x.Dropships > 0 && x.DropshipsLastAdded != null))
        {
            if (DateTime.UtcNow.Subtract(pf.DropshipsLastAdded.Value).TotalMinutes > GlobalConst.PlanetWarsDropshipsStayForMinutes)
            {
                pf.Faction.ProduceDropships(pf.Dropships);
                pf.Dropships = 0;
            }
        }


        // add attack points
        foreach (Account acc in players)
        {
            int ap = acc.Faction == attacker ? GlobalConst.AttackPointsForVictory : GlobalConst.AttackPointsForDefeat;
            if (acc.Faction != null)
            {
                AccountPlanet apentry = planet.AccountPlanets.SingleOrDefault(x => x.AccountID == acc.AccountID);
                if (apentry == null)
                {
                    apentry = new AccountPlanet { AccountID = acc.AccountID, PlanetID = planet.PlanetID };
                    db.AccountPlanets.InsertOnSubmit(apentry);
                }

                apentry.AttackPoints += ap;
            }
            acc.PwAttackPoints += ap;
        }

        // paranoia!
        try
        {
            var mainEvent = eventCreator.CreateEvent("{0} attacked {1} {2} in {3} and {4}. {5}",
                attacker,
                planet.Faction,
                planet,
                sb,
                isAttackerWinner ? "won. " : "lost. ",
                influenceReport
                );
            db.Events.InsertOnSubmit(mainEvent);
            text.AppendLine(mainEvent.PlainText);

        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
        }

        // destroy pw structures killed ingame
        if (!isAttackerWinner)
        {
            var handled = new List<string>();
            foreach (string line in extraData.Where(x => x.StartsWith("structurekilled")))
            {
                string[] data = line.Substring(16).Split(',');
                string unitName = data[0];
                if (handled.Contains(unitName)) continue;
                handled.Add(unitName);
                foreach (PlanetStructure s in planet.PlanetStructures.Where(x => x.StructureType.IngameUnitName == unitName))
                {
                    if (s.StructureType.IsIngameDestructible)
                    {
                        s.IsActive = false;
                        s.ActivatedOnTurn = gal.Turn + (int)(s.StructureType.TurnsToActivate * (GlobalConst.StructureIngameDisableTimeMult - 1));

                        var ev = eventCreator.CreateEvent("{0} has been disabled on {1} planet {2}. {3}", s.StructureType.Name, planet.Faction, planet, sb);
                        db.Events.InsertOnSubmit(ev);
                        text.AppendLine(ev.PlainText);
                    }
                }
            }
        }
        else
        {
            // attacker won disable all
            foreach (var s in planet.PlanetStructures.Where(x => x.StructureType.IsIngameDestructible))
            {
                s.IsActive = false;
                s.ActivatedOnTurn = gal.Turn + (int)(s.StructureType.TurnsToActivate * (GlobalConst.StructureIngameDisableTimeMult - 1));
            }
            // destroy structures by battle (usually defenses)
            foreach (PlanetStructure s in planet.PlanetStructures.Where(x => x.StructureType.BattleDeletesThis).ToList()) planet.PlanetStructures.Remove(s);

            var ev = eventCreator.CreateEvent("All structures have been disabled on {0} planet {1}. {2}", planet.Faction, planet, sb);
            db.Events.InsertOnSubmit(ev);
            text.AppendLine(ev.PlainText);
        }

        db.SaveChanges();

        gal.DecayInfluence();
        gal.SpreadInfluence();

        // process faction energies
        foreach (var fac in db.Factions.Where(x => !x.IsDeleted)) fac.ProcessEnergy(gal.Turn);

        // process production
        gal.ProcessProduction();


        // process treaties
        foreach (var tr in db.FactionTreaties.Where(x => x.TreatyState == TreatyState.Accepted || x.TreatyState == TreatyState.Suspended))
        {
            if (tr.ProcessTrade(false))
            {
                tr.TreatyState = TreatyState.Accepted;
                if (tr.TurnsTotal != null)
                {
                    tr.TurnsRemaining--;
                    if (tr.TurnsRemaining <= 0)
                    {
                        tr.TreatyState = TreatyState.Invalid;
                        db.FactionTreaties.DeleteOnSubmit(tr);
                    }
                }
            }
            else tr.TreatyState = TreatyState.Suspended;
        }

        // burn extra energy
        foreach (var fac in db.Factions.Where(x => !x.IsDeleted)) fac.ConvertExcessEnergyToMetal();


        int? oldOwner = planet.OwnerAccountID;
        gal.Turn++;
        db.SaveChanges();

        db = new ZkDataContext(); // is this needed - attempt to fix setplanetownersbeing buggy
        SetPlanetOwners(eventCreator, db, sb != null ? db.SpringBattles.Find(sb.SpringBattleID) : null);
        gal = db.Galaxies.Single(x => x.IsDefault);

        planet = gal.Planets.Single(x => x.Resource.InternalName == mapName);
        if (planet.OwnerAccountID != oldOwner && planet.OwnerAccountID != null)
        {
            text.AppendFormat("Congratulations!! Planet {0} was conquered by {1} !!  {3}/PlanetWars/Planet/{2}\n",
                planet.Name,
                planet.Account.Name,
                planet.PlanetID,
                GlobalConst.BaseSiteUrl);
        }

        try
        {

            // store history
            foreach (Planet p in gal.Planets)
            {
                db.PlanetOwnerHistories.InsertOnSubmit(new PlanetOwnerHistory
                {
                    PlanetID = p.PlanetID,
                    OwnerAccountID = p.OwnerAccountID,
                    OwnerClanID = p.OwnerAccountID != null ? p.Account.ClanID : null,
                    OwnerFactionID = p.OwnerFactionID,
                    Turn = gal.Turn
                });
            }

            db.SaveChanges();
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
            text.AppendLine("error saving history: " + ex);
        }

        //rotate map
        if (GlobalConst.RotatePWMaps)
        {
            db = new ZkDataContext();
            gal = db.Galaxies.Single(x => x.IsDefault);
            planet = gal.Planets.Single(x => x.Resource.InternalName == mapName);
            var mapList = db.Resources.Where(x => x.MapPlanetWarsIcon != null && x.Planets.Where(p => p.GalaxyID == gal.GalaxyID).Count() == 0 && x.MapSupportLevel>=MapSupportLevel.Featured
                                                  && x.ResourceID != planet.MapResourceID && x.MapWaterLevel == planet.Resource.MapWaterLevel).ToList();
            if (mapList.Count > 0)
            {
                int r = new Random().Next(mapList.Count);
                int resourceID = mapList[r].ResourceID;
                Resource newMap = db.Resources.Single(x => x.ResourceID == resourceID);
                text.AppendLine(String.Format("Map cycler - {0} maps found, selected map {1} to replace map {2}", mapList.Count, newMap.InternalName, planet.Resource.InternalName));
                planet.Resource = newMap;
                gal.IsDirty = true;
            }
            else
            {
                text.AppendLine("Map cycler - no maps found");
            }
            db.SaveChanges();
        }
    }

    /// <summary>
    /// Updates shadow influence and new owners
    /// </summary>
    /// <param name="db"></param>
    /// <param name="sb">optional spring batle that caused this change (for event logging)</param>
    public static void SetPlanetOwners(IPlanetwarsEventCreator eventCreator, ZkDataContext db = null, SpringBattle sb = null)
    {
        if (db == null) db = new ZkDataContext();

        Galaxy gal = db.Galaxies.Single(x => x.IsDefault);
        foreach (Planet planet in gal.Planets)
        {
            if (planet.OwnerAccountID != null) foreach (var ps in planet.PlanetStructures.Where(x => x.OwnerAccountID == null))
                {
                    ps.OwnerAccountID = planet.OwnerAccountID;
                    ps.IsActive = false;
                    ps.ActivatedOnTurn = null;
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
                    structure.IsActive = false;
                    structure.ActivatedOnTurn = null;
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
                }

                planet.Faction = newFaction;
                planet.Account = newAccount;
            }
            ReturnPeacefulDropshipsHome(db, planet);
        }
        db.SaveChanges();
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