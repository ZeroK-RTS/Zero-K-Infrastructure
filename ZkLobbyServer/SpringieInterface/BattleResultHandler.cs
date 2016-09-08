﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using LobbyClient;
using Newtonsoft.Json;
using PlasmaShared;
using ZkData;

namespace ZeroKWeb.SpringieInterface
{
    /// <summary>
    /// Handle everything to do with battle results: change Elo and XP, awards, PlanetWars effects...
    /// </summary>
    public class BattleResultHandler
    {
        public static string SubmitSpringBattleResult(Spring.SpringBattleContext result, ZkLobbyServer.ZkLobbyServer server)
        {
            try
            {
                if (!result.GameEndedOk) return "Game didn't end properly";
                if (result.IsCheating) return "Cheats were enabled during this game";

                var db = new ZkDataContext();
                var context = result.LobbyStartContext;
                AutohostMode mode = result.LobbyStartContext.Mode;


                var sb = new SpringBattle
                         {
                             HostAccountID = Account.AccountByName(db, result.LobbyStartContext.FounderName)?.AccountID,
                             Mode = result.LobbyStartContext.Mode,
                             Duration = result.Duration,
                             EngineGameID = result.EngineBattleID,
                             MapResourceID = db.Resources.Single(x => x.InternalName == context.Map).ResourceID,
                             ModResourceID = db.Resources.Single(x => x.InternalName == context.Mod).ResourceID,
                             HasBots = context.Bots.Any(),
                             IsMission = context.IsMission,
                             PlayerCount = result.ActualPlayers.Count(x => !x.IsSpectator),
                             StartTime = result.StartTime,
                             Title = context.Title,
                             ReplayFileName = result.ReplayName,
                             EngineVersion = context.EngineVersion,
                         };
                db.SpringBattles.InsertOnSubmit(sb);

                foreach (BattlePlayerResult p in result.ActualPlayers)
                {
                    sb.SpringBattlePlayers.Add(new SpringBattlePlayer
                                               {
                                                   AccountID =  Account.AccountByName(db, p.Name).AccountID,
                                                   AllyNumber = p.AllyNumber,
                                                   IsInVictoryTeam = p.IsVictoryTeam,
                                                   IsSpectator = p.IsSpectator,
                                                   LoseTime = p.LoseTime
                                               });
                }

                db.SaveChanges();

                // awards
                foreach (string line in result.OutputExtras.Where(x => x.StartsWith("award")))
                {
                    string[] partsSpace = line.Substring(6).Split(new[] { ' ' }, 3);
                    string name = partsSpace[0];
                    string awardType = partsSpace[1];
                    string awardText = partsSpace[2];

                    // prevent hax: tourney cups and event coins are never given automatically
                    if (awardType != null && (awardType == "gold" || awardType == "silver" || awardType == "bronze")) continue;
                    if (awardType != null && (awardType == "goldcoin" || awardType == "silvercoin" || awardType == "bronzecoin")) continue;

                    SpringBattlePlayer player = sb.SpringBattlePlayers.FirstOrDefault(x => x.Account.Name == name);
                    if (player != null)
                    {
                        db.AccountBattleAwards.InsertOnSubmit(new AccountBattleAward
                                                              {
                                                                  AccountID = player.AccountID,
                                                                  SpringBattleID = sb.SpringBattleID,
                                                                  AwardKey = awardType,
                                                                  AwardDescription = awardText
                                                              });
                    }
                }

                // chatlogs
                foreach (string line in result.OutputExtras.Where(x => x?.StartsWith("CHATLOG") == true))
                {
                    string[] partsSpace = line.Substring(8).Split(new[] { ' ' }, 2);
                    string name = partsSpace[0];
                    string chatlog = partsSpace[1];

                    SpringBattlePlayer player = sb.SpringBattlePlayers.FirstOrDefault(x => x.Account.Name == name);
                    if (player != null)
                    {
                        db.LobbyChatHistories.InsertOnSubmit(new LobbyChatHistory
                                                              {
                                                                  IsEmote = false,
                                                                  SayPlace = SayPlace.Game,
                                                                  User = name,
                                                                  Ring = false,
                                                                  Target = "B" + sb.SpringBattleID,
                                                                  Text = chatlog,
                                                                  Time = DateTime.UtcNow
                                                              });
                    }
                }

                var text = new StringBuilder();
                bool isPlanetwars = false;
                if (mode == AutohostMode.Planetwars && sb.SpringBattlePlayers.Count(x => !x.IsSpectator) >= 2 && sb.Duration >= GlobalConst.MinDurationForPlanetwars)
                {
                    // test that factions are not intermingled (each faction only has one ally number) - if they are it wasnt actually PW balanced
                    if (
                        sb.SpringBattlePlayers.Where(x => !x.IsSpectator && x.Account.Faction != null)
                          .GroupBy(x => x.Account.Faction)
                          .All(grp => grp.Select(x => x.AllyNumber).Distinct().Count() < 2))
                    {
                        isPlanetwars = true;

                        
                        List<int> winnerTeams =
    sb.SpringBattlePlayers.Where(x => x.IsInVictoryTeam && !x.IsSpectator).Select(x => x.AllyNumber).Distinct().ToList();
                        int? winNum = null;
                        if (winnerTeams.Count == 1)
                        {
                            winNum = winnerTeams[0];
                            if (winNum > 1)
                            {
                                winNum = null;
                                text.AppendLine("ERROR: Invalid winner");
                            } 
                        }

                        PlanetWarsTurnHandler.EndTurn(context.Map, result.OutputExtras, db, winNum, sb.SpringBattlePlayers.Where(x => !x.IsSpectator).Select(x => x.Account).ToList(), text, sb, sb.SpringBattlePlayers.Where(x => !x.IsSpectator && x.AllyNumber == 0).Select(x => x.Account).ToList(), server.PlanetWarsEventCreator);

                        // TODO HACK Global.PlanetWarsMatchMaker.RemoveFromRunningBattles(context.AutohostName);
                    }
                    else
                    {
                        text.AppendLine("Battle wasn't PlanetWars balanced, it counts as a normal team game only");
                    }

                }

                bool noElo = (result.OutputExtras.FirstOrDefault(x => x.StartsWith("noElo", true, System.Globalization.CultureInfo.CurrentCulture)) != null);
                try
                {
                    db.SaveChanges();
                }
                catch (System.Data.Linq.DuplicateKeyException ex)
                {
                    Trace.TraceError(ex.ToString());
                }

                Dictionary<int, int> orgLevels = sb.SpringBattlePlayers.Select(x => x.Account).ToDictionary(x => x.AccountID, x => x.Level);

                sb.CalculateAllElo(noElo, isPlanetwars);
                foreach (var u in sb.SpringBattlePlayers.Where(x => !x.IsSpectator)) u.Account.CheckLevelUp();

                db.SaveChanges();

                try
                {
                    foreach (Account a in sb.SpringBattlePlayers.Where(x => !x.IsSpectator).Select(x => x.Account)) server.PublishAccountUpdate(a);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("error updating extension data: {0}", ex);
                }

                foreach (Account account in sb.SpringBattlePlayers.Select(x => x.Account))
                {
                    if (account.Level > orgLevels[account.AccountID])
                    {
                        try
                        {
                            string message =
                                string.Format("Congratulations {0}! You just leveled up to level {1}. {3}/Users/Detail/{2}",
                                              account.Name,
                                              account.Level,
                                              account.AccountID,
                                              GlobalConst.BaseSiteUrl);
                            //text.AppendLine(message);
                            server.GhostPm(account.Name, message);
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError("Error sending level up lobby message: {0}", ex);
                        }
                    }
                }

                text.AppendLine(string.Format("BATTLE DETAILS AND REPLAY ----> {1}/Battles/Detail/{0} <-----", sb.SpringBattleID, GlobalConst.BaseSiteUrl));
                return text.ToString();
            }
            catch (Exception ex)
            {
                var data = JsonConvert.SerializeObject(result);
                Trace.TraceError($"{ex}\nData:\n{data}");
                return $"{ex}\nData:\n{data}";
            }
        }
        
    }
}
