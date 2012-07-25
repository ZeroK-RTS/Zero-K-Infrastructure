using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web;
using LobbyClient;
using PlasmaShared;
using ZeroKWeb.Controllers;
using ZkData;

namespace ZeroKWeb.SpringieInterface
{
    public class BattlePlayerResult
    {
        public int LobbyID;
        public int AllyNumber;
        public string CommanderType;
        public bool IsIngameReady;
        public bool IsSpectator;
        public bool IsVictoryTeam;
        public int? LoseTime;
        public int Rank;
    }

    public class BattleResult
    {
        public int Duration;
        public string EngineBattleID;
        public string EngineVersion;
        public DateTime? IngameStartTime;
        public bool IsBots;
        public bool IsMission;
        public string Map;
        public string Mod;
        public string ReplayName;
        public DateTime StartTime;
        public string Title;
    }


    public class BattleResultHandler
    {

        public static string SubmitSpringBattleResult(BattleContext context, string password,
                                               BattleResult result,
                                               List<BattlePlayerResult> players,
                                               List<string> extraData)
        {
            try
            {
                var acc = AuthServiceClient.VerifyAccountPlain(context.AutohostName, password);
                if (acc == null) throw new Exception("Account name or password not valid");
                var mode = context.GetMode();
                var db = new ZkDataContext();
              

                if (extraData == null) extraData = new List<string>();

                if (mode == AutohostMode.Planetwars) db.ExecuteCommand("update account set creditsincome =0, creditsexpense=0 where creditsincome<>0 or creditsexpense<>0");

                var sb = new SpringBattle()
                {
                    HostAccountID = acc.AccountID,
                    Duration = result.Duration,
                    EngineGameID = result.EngineBattleID,
                    MapResourceID = db.Resources.Single(x => x.InternalName == result.Map).ResourceID,
                    ModResourceID = db.Resources.Single(x => x.InternalName == result.Mod).ResourceID,
                    HasBots = result.IsBots,
                    IsMission = result.IsMission,
                    PlayerCount = players.Count(x => !x.IsSpectator),
                    StartTime = result.StartTime,
                    Title = result.Title,
                    ReplayFileName = result.ReplayName,
                    EngineVersion = result.EngineVersion,
                };
                db.SpringBattles.InsertOnSubmit(sb);

                foreach (var p in players)
                {
                    sb.SpringBattlePlayers.Add(new SpringBattlePlayer()
                    {
                        AccountID = db.Accounts.First(x => x.LobbyID == p.LobbyID).AccountID,
                        AllyNumber = p.AllyNumber,
                        CommanderType = p.CommanderType,
                        IsInVictoryTeam = p.IsVictoryTeam,
                        IsSpectator = p.IsSpectator,
                        Rank = p.Rank,
                        LoseTime = p.LoseTime
                    });
                }

                db.SubmitChanges();

                // awards
                foreach (var line in extraData.Where(x => x.StartsWith("award")))
                {
                    var partsSpace = line.Substring(6).Split(new[] { ' ' }, 3);
                    var name = partsSpace[0];
                    var awardType = partsSpace[1];
                    var awardText = partsSpace[2];

                    var player = sb.SpringBattlePlayers.FirstOrDefault(x => x.Account.Name == name && x.Account.LobbyID != null);
                    if (player != null) {
                        db.AccountBattleAwards.InsertOnSubmit(new AccountBattleAward()
                                                                  { AccountID = player.AccountID, SpringBattleID = sb.SpringBattleID, AwardKey = awardType, AwardDescription = awardText });
                    }
                }
                db.SubmitChanges();

                var orgLevels = sb.SpringBattlePlayers.Select(x => x.Account).ToDictionary(x => x.AccountID, x => x.Level);

                sb.CalculateElo();
                try
                {
                    db.SubmitChanges();
                }
                catch (ChangeConflictException e)
                {
                    db.ChangeConflicts.ResolveAll(RefreshMode.KeepChanges);
                    db.SubmitChanges();
                }

                try {
                    foreach (var a in sb.SpringBattlePlayers.Where(x=>!x.IsSpectator).Select(x => x.Account)) Global.Nightwatch.Tas.Extensions.PublishAccountData(a);
                } catch (Exception ex) {
                    Trace.TraceError("error updating extension data: {0}",ex);
                }

                var text = new StringBuilder();

                if (mode == AutohostMode.Planetwars && sb.SpringBattlePlayers.Any())
                {
                    var gal = db.Galaxies.Single(x => x.IsDefault);
                    var planet = gal.Planets.Single(x => x.MapResourceID == sb.MapResourceID);

                    text.AppendFormat("Battle on http://zero-k.info/PlanetWars/Planet/{0} has ended\n", planet.PlanetID);

                    // handle influence
                    Faction attacker = null;
                    Faction defender = null;
                    //Clan ownerClan = null;
                    var involvedClans = new List<Clan>();
                    if (planet.Account != null)
                    {
                        //ownerClan = planet.Account.Clan;
                        defender = planet.Account.Faction;
                        //attacker = ??
                        //if (ownerClan != null) involvedClans.Add(ownerClan); // planet ownerinvolved
                    }

                    // ship owners -> involved
                    var activePlayerIds =
                        sb.SpringBattlePlayers.Where(x => !x.IsSpectator && x.Account.FactionID != null).Select(x => x.AccountID).ToList();
                    bool wasShipAttacked = false;
                    foreach (
                        var c in
                            planet.PlanetFactions.Where(
                                x => x.Dropships > 0))
                    {
                        wasShipAttacked = true;
                    }
                    if (!wasShipAttacked) involvedClans.Clear(); // insurgency no involved

                    var factionTechIP = sb.SpringBattlePlayers.Where(x => !x.IsSpectator).Select(x => x.Account).Where(x => x.ClanID != null).GroupBy(x => x.ClanID).
                            ToDictionary(x => x.Key, z => Galaxy.ClanUnlocks(db, z.Key).Count() * 6.0 / z.Count()); // FIXME make techs in db be by faction not clan!

                    var planetDefs = (planet.PlanetStructures.Where(x => x.IsActive).Sum(x => x.StructureType.EffectDropshipDefense) ?? 0);
                    var totalShips = (planet.PlanetFactions.Sum(x => (int?)x.Dropships) ?? 0);
                    double shipMultiplier = 1;
                    if (totalShips > 0 && totalShips >= planetDefs) shipMultiplier = (totalShips - planetDefs) / (double)totalShips;

                    /*
                    var ownerMalus = 0;
                    if (defender != null)
                    {
                        var entries = planet.GetFactionInfluences();
                        if (entries.Count() > 1)
                        {
                            var diff = entries.First().Influence - entries.Skip(1).First().Influence;
                            ownerMalus = Math.Min((int)((diff / 100.0) * (diff / 100.0)), 70);
                        }
                    }
                    */

                    // malus for ships
                    /*
                    foreach (var p in sb.SpringBattlePlayers.Where(x => !x.IsSpectator && !x.IsInVictoryTeam && x.Account.FactionID != null))
                    {
                        var ships = planet.AccountPlanets.Where(x => x.AccountID == p.AccountID).Sum(x => (int?)x.DropshipCount) ?? 0;
                        if (ships <= 0) continue;
                        p.Influence = ships * GlobalConst.InfluenceMalusPerShip;
                        var entry = planet.AccountPlanets.SingleOrDefault(x => x.AccountID == p.AccountID);
                        if (entry == null)
                        {
                            entry = new AccountPlanet() { AccountID = p.AccountID, PlanetID = planet.PlanetID };
                            db.AccountPlanets.InsertOnSubmit(entry);
                        }
                        entry.Influence += p.Influence ?? 0;

                        db.Events.InsertOnSubmit(Global.CreateEvent("{0} lost {1} influence at {2} because of {3} ships {4}",
                                                                    p.Account,
                                                                    p.Influence ?? 0,
                                                                    planet,
                                                                    ships,
                                                                    sb));

                        text.AppendFormat("{0} lost {1} influence at {2} because of {3} ships\n", p.Account.Name, p.Influence ?? 0, planet.Name, ships);
                    }
                    */

                    // determine which players are involved and count them
                    int numInvolved = 0;
                    int numInvolvedWinner = 0;
                    int numShips = 0;
                    var involvedPlayers = new List<SpringBattlePlayer>();
                    foreach (var p in sb.SpringBattlePlayers.Where(x => !x.IsSpectator && (x.Account.FactionID == attacker.FactionID || x.Account.FactionID == defender.FactionID)))
                    {
                        numInvolved++;
                        involvedPlayers.Add(p);
                        if (p.IsInVictoryTeam)
                        {
                            numInvolvedWinner++;
                        }
                    }

                    // FIXME: doesn't take techs into account!
                    double totalInfluence = GlobalConst.BaseInfluencePerBattle + (numShips * GlobalConst.InfluencePerShip * shipMultiplier) + (numInvolved * GlobalConst.InfluencePerInvolvedPlayer);

                    foreach (var p in sb.SpringBattlePlayers.Where(x => !x.IsSpectator && x.IsInVictoryTeam && involvedPlayers.Contains(x)))
                    {
                        // this is for the benefit of the after-action text report
                        var techBonus = p.Account.ClanID != null ? (int)factionTechIP[p.Account.ClanID] : 0;
                        //var gapMalus = 0;
                        var shipBonus = (numShips * GlobalConst.InfluencePerShip * shipMultiplier / numInvolvedWinner);
                        //if (defender != null && p.Account.Faction == defender) gapMalus = ownerMalus;
                        //p.Influence += (techBonus - gapMalus);

                        p.Influence += (int)Math.Round(totalInfluence/numInvolvedWinner);

                        if (p.Influence < 0) p.Influence = 0;

                        var entry = planet.AccountPlanets.SingleOrDefault(x => x.AccountID == p.AccountID);
                        if (entry == null)
                        {
                            entry = new AccountPlanet() { AccountID = p.AccountID, PlanetID = planet.PlanetID };
                            db.AccountPlanets.InsertOnSubmit(entry);
                        }

                        entry.AttackPoints++; // todo difference between win and lose
                        
                        /*
                        if (isInvolved) entry.Influence += infl;
                        
                        else
                        {
                            p.Account.Credits += infl * GlobalConst.NotInvolvedIpSell;
                            soldStr = string.Format("sold for ${0} to locals because wasn't directly involved", infl * GlobalConst.NotInvolvedIpSell);
                        }
                        */

                        db.Events.InsertOnSubmit(Global.CreateEvent("{0} got {1} ({4} {5}) influence at {2} from {3} ",
                                                                    p.Account,
                                                                    p.Influence ?? 0,
                                                                    planet,
                                                                    sb,
                                                                    techBonus > 0 ? "+" + techBonus + " from techs" : "",
                                                                    //gapMalus > 0 ? "-" + gapMalus + " from domination" : "",
                                                                    shipBonus > 0 ? "+" + shipBonus + " from ships" : ""
                                                                    ));

                        text.AppendFormat("{0} got {1} ({3} {4}) influence at {2}\n",
                                          p.Account.Name,
                                          p.Influence ?? 0,
                                          planet.Name,
                                          techBonus > 0 ? "+" + techBonus + " from techs" : "",
                                          //gapMalus > 0 ? "-" + gapMalus + " from domination" : "",
                                          shipBonus > 0 ? "+" + shipBonus + " from ships" : ""
                                          );
                    }

                    db.SubmitChanges();

                    // destroy existing dropships and prevent growth
                    var noGrowFaction = new List<int>();
                    if (result.Duration > 360)
					{
						foreach (var ap in planet.PlanetFactions.Where(x => x.Dropships > 0))
						{
							ap.Dropships = 0;
						}
					}

                    // destroy pw structures
                    // TODO: support for "no destroy PW structures treaty?"
                    var handled = new List<string>();
                    foreach (var line in extraData.Where(x => x.StartsWith("structurekilled")))
                    {
                        var data = line.Substring(16).Split(',');
                        var unitName = data[0];
                        if (handled.Contains(unitName)) continue;
                        handled.Add(unitName);
                        foreach (var s in
                            db.PlanetStructures.Where(x => x.PlanetID == planet.PlanetID && x.StructureType.IngameUnitName == unitName && x.IsActive))
                        {
                            if (s.StructureType.IsIngameDestructible)
                            {
                                    db.PlanetStructures.DeleteOnSubmit(s);
                                db.Events.InsertOnSubmit(Global.CreateEvent("{0} has been destroyed on {1} planet {2}. {3}",
                                                                            s.StructureType.Name,
                                                                            defender,
                                                                            planet,
                                                                            sb));
                            }
                        }
                    }
                    db.SubmitChanges();

                    // TODO correct IP after battle

                    // destroy structures (usually defenses)
                    foreach (var s in planet.PlanetStructures.Where(x => x.IsActive && x.StructureType.BattleDeletesThis).ToList()) planet.PlanetStructures.Remove(s);
                    db.SubmitChanges();

                    // spawn new dropships
                    /*
                    foreach (var a in
                        sb.SpringBattlePlayers.Where(x => !x.IsSpectator).Select(x => x.Account).Where(
                            x => x.ClanID != null && !noGrowAccount.Contains(x.AccountID)))
                    {
                        var income = GlobalConst.DefaultDropshipProduction +
                                     (a.PlanetStructures.Where(x => x.IsActive && x.IsActive).Sum(
                                         x => x.StructureType.EffectDropshipProduction) ?? 0);

                        a.ProduceDropships(income);
                    }
                    db.SubmitChanges();*/

                    db.SubmitChanges();

                    //  TODO: IP squeeze if overflow
                    db = new ZkDataContext();
                    db.SubmitChanges();
                    

/*                    // kill structures you cannot support 
                    foreach (var owner in gal.Planets.Where(x => x.Account != null).Select(x => x.Account).Distinct())
                    {
                        if (owner.Credits < 0)
                        {
                            var upkeepStructs =
                                owner.Planets.SelectMany(x => x.PlanetStructures).Where(
                                    x => x.IsActive && x.StructureType.UpkeepCost > 0 && x.StructureType.EffectIsVictoryPlanet != true).
                                    OrderByDescending(x => x.StructureType.UpkeepCost);
                            var structToKill = upkeepStructs.FirstOrDefault();
                            if (structToKill != null)
                            {
                                structToKill.IsDestroyed = true;
                                owner.Credits += structToKill.StructureType.UpkeepCost;
                                db.Events.InsertOnSubmit(Global.CreateEvent("{0} on {1}'s planet {2} has been destroyed due to lack of upkeep",
                                                                            structToKill.StructureType.Name,
                                                                            owner,
                                                                            structToKill.Planet));
                            }
                        }
                    }*/

                    var oldOwner = planet.OwnerAccountID;
                    gal.Turn++;
                    db.SubmitChanges();

                    /*
                    // give unclanned influence to clanned
                    if (GlobalConst.GiveUnclannedInfluenceToClanned)
                    {
                        db = new ZkDataContext();
                        planet = db.Planets.Single(x => x.PlanetID == planet.PlanetID);
                        foreach (var faction in
                            planet.AccountPlanets.Where(x => x.Account.FactionID != null && x.Influence > 0).GroupBy(x => x.Account.FactionID))
                        {
                            var unclanned = faction.Where(x => x.Account.ClanID == null).ToList();
                            var clanned = faction.Where(x => x.Account.ClanID != null).ToList();
                            var unclannedInfluence = 0;
                            if (unclanned.Any() && clanned.Any() && (unclannedInfluence = unclanned.Sum(x => x.Influence)) > 0)
                            {
                                var influenceBonus = unclannedInfluence / clanned.Count();
                                foreach (var clannedEntry in clanned) clannedEntry.Influence += influenceBonus;
                                foreach (var unclannedEntry in unclanned) unclannedEntry.Influence = 0;
                            }
                        }
                        db.SubmitChanges();
                    }

                    // transfer ceasefire/alliance influences
                    if (defender != null)
                    {
                        db = new ZkDataContext();
                        planet = db.Planets.Single(x => x.PlanetID == planet.PlanetID);

                        var ownerEntries = planet.AccountPlanets.Where(x => x.Influence > 0 && x.Account.ClanID == ownerClan.ClanID).ToList();
                        if (ownerEntries.Any())
                        {
                            foreach (var clan in
                                planet.AccountPlanets.Where(
                                    x =>
                                    x.Account.ClanID != null && x.Account.ClanID != ownerClan.ClanID && x.Influence > 0 &&
                                    x.Account.Clan.FactionID != ownerClan.FactionID).GroupBy(x => x.Account.Clan))
                            {
                                // get clanned influences of other than owner clans of different factions

                                var treaty = clan.Key.GetEffectiveTreaty(ownerClan);
                                if (treaty.AllyStatus == AllyStatus.Alliance ||
                                    (treaty.AllyStatus == AllyStatus.Ceasefire &&
                                     treaty.InfluenceGivenToSecondClanBalance < GlobalConst.CeasefireMaxInfluenceBalanceRatio))
                                {
                                    // if we are allied or ceasefired and influence balance < 150%, send ifnluence to owners
                                    var total = clan.Sum(x => x.Influence);
                                    var increment = total / ownerEntries.Count();
                                    foreach (var e in ownerEntries) e.Influence += increment;
                                    foreach (var e in clan) e.Influence = 0;

                                    var offer =
                                        db.TreatyOffers.SingleOrDefault(x => x.OfferingClanID == clan.Key.ClanID && x.TargetClanID == ownerClan.ClanID);
                                    if (offer == null)
                                    {
                                        offer = new TreatyOffer() { OfferingClanID = clan.Key.ClanID, TargetClanID = ownerClan.ClanID };
                                        db.TreatyOffers.InsertOnSubmit(offer);
                                    }
                                    offer.InfluenceGiven += total;

                                    db.Events.InsertOnSubmit(Global.CreateEvent("{0} gave {1} influence on {2} to clan {3} because of their treaty",
                                                                                clan.Key,
                                                                                total,
                                                                                planet,
                                                                                ownerClan));
                                }
                            }
                        }
                        db.SubmitChanges();
                    }
                    */
                     
                    db = new ZkDataContext(); // is this needed - attempt to fix setplanetownersbeing buggy
                    PlanetwarsController.SetPlanetOwners(db, sb);
                    gal = db.Galaxies.Single(x => x.IsDefault);

              

                    planet = gal.Planets.Single(x => x.Resource.InternalName == result.Map);
                    if (planet.OwnerAccountID != oldOwner && planet.OwnerAccountID != null)
                    {
                        text.AppendFormat("Congratulations!! Planet {0} was conquered by {1} !!  http://zero-k.info/PlanetWars/Planet/{2}\n",
                                          planet.Name,
                                          planet.Account.Name,
                                          planet.PlanetID);
                    }

                    try
                    {
                        // store history
                        foreach (var p in gal.Planets)
                        {
                            db.PlanetOwnerHistories.InsertOnSubmit(new PlanetOwnerHistory()
                            {
                                PlanetID = p.PlanetID,
                                OwnerAccountID = p.OwnerAccountID,
                                OwnerClanID = p.OwnerAccountID != null ? p.Account.ClanID : null,
                                OwnerFactionID = p.OwnerFactionID,
                                Turn = gal.Turn
                            });

                           
                        }

                        db.SubmitChanges();
                    }
                    catch (Exception ex)
                    {
                        text.AppendLine("error saving history: " + ex.ToString());
                    }
                }

                foreach (var account in sb.SpringBattlePlayers.Select(x => x.Account))
                {
                    if (account.Level > orgLevels[account.AccountID])
                    {
                        try
                        {
                            var message = string.Format("Congratulations {0}! You just leveled up to level {1}. http://zero-k.info/Users/Detail/{2}",
                                                        account.Name,
                                                        account.Level,
                                                        account.AccountID);
                            text.AppendLine(message);
                            AuthServiceClient.SendLobbyMessage(account, message);
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError("Error sending level up lobby message: {0}", ex);
                        }
                    }
                }

                text.AppendLine(string.Format("Debriefing room for a battle at {0}", context.Map));
                text.AppendLine(string.Format("View full battle details and demo at http://zero-k.info/Battles/Detail/{0}", sb.SpringBattleID));
                

                // create debriefing room, join players there and output message
                var channelName = "B" + sb.SpringBattleID;
                var joinplayers = new List<string>();
                joinplayers.AddRange(context.Players.Select(x=>x.Name)); // add those who were there at start
                joinplayers.AddRange(sb.SpringBattlePlayers.Select(x => x.Account.Name)); // add those who played
                var tas = Global.Nightwatch.Tas;
                var bat=  tas.ExistingBattles.Values.FirstOrDefault(x => x.Founder.Name == context.AutohostName); // add those in lobby atm
                
                if (bat != null)
                {
                    var inbatPlayers = bat.Users.Select(x => x.Name).ToList();
                    joinplayers.RemoveAll(x=>inbatPlayers.Contains(x));
                }
                foreach (var jp in joinplayers.Distinct().Where(x=>x!=context.AutohostName)) tas.ForceJoinChannel(jp, channelName);
                tas.JoinChannel(channelName); // join nightwatch and say it
                tas.Say(TasClient.SayPlace.Channel, channelName, text.ToString(), true);
                tas.LeaveChannel(channelName);

                text.Append(string.Format("Debriefing in #{0} - spring://chat/channel/{0}  ", channelName));
                return text.ToString();
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }
    }
}