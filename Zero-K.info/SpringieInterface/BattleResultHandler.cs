using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using LobbyClient;
using ZeroKWeb.Controllers;
using ZkData;

namespace ZeroKWeb.SpringieInterface
{
    public class BattlePlayerResult
    {
        public int AllyNumber;
        public string CommanderType;
        public bool IsIngameReady;
        public bool IsSpectator;
        public bool IsVictoryTeam;
        public int LobbyID;
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
        public static string SubmitSpringBattleResult(BattleContext context,
                                                      string password,
                                                      BattleResult result,
                                                      List<BattlePlayerResult> players,
                                                      List<string> extraData)
        {
            try
            {
                Account acc = AuthServiceClient.VerifyAccountPlain(context.AutohostName, password);
                if (acc == null) throw new Exception("Account name or password not valid");
                AutohostMode mode = context.GetMode();
                var db = new ZkDataContext();

                if (extraData == null) extraData = new List<string>();


                var sb = new SpringBattle
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

                foreach (BattlePlayerResult p in players)
                {
                    sb.SpringBattlePlayers.Add(new SpringBattlePlayer
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
                foreach (string line in extraData.Where(x => x.StartsWith("award")))
                {
                    string[] partsSpace = line.Substring(6).Split(new[] { ' ' }, 3);
                    string name = partsSpace[0];
                    string awardType = partsSpace[1];
                    string awardText = partsSpace[2];

                    SpringBattlePlayer player = sb.SpringBattlePlayers.FirstOrDefault(x => x.Account.Name == name && x.Account.LobbyID != null);
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

                        PlanetWarsTurnHandler.EndTurn(result.Map, extraData, db, winNum, sb.SpringBattlePlayers.Where(x => !x.IsSpectator).Select(x => x.Account).ToList(), text, sb, sb.SpringBattlePlayers.Where(x => !x.IsSpectator && x.AllyNumber == 0).Select(x => x.Account).ToList());

                        Global.PlanetWarsMatchMaker.RemoveFromRunningBattles(context.AutohostName);
                    }
                    else
                    {
                        text.AppendLine("Battle wasn't PlanetWars balanced, it counts as a normal team game only");
                    }

                }

                bool noElo = (extraData.FirstOrDefault(x => x.StartsWith("noElo")) != null);
                try
                {
                    db.SubmitChanges();
                }
                catch (System.Data.Linq.DuplicateKeyException ex)
                {
                    Global.Nightwatch.Tas.Say(TasClient.SayPlace.User, "KingRaptor", ex.ToString(), false);
                }

                Dictionary<int, int> orgLevels = sb.SpringBattlePlayers.Select(x => x.Account).ToDictionary(x => x.AccountID, x => x.Level);

                sb.CalculateAllElo(noElo, isPlanetwars);
                db.SubmitAndMergeChanges();

                try
                {
                    foreach (Account a in sb.SpringBattlePlayers.Where(x => !x.IsSpectator).Select(x => x.Account)) Global.Nightwatch.Tas.Extensions.PublishAccountData(a);
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
                                string.Format("Congratulations {0}! You just leveled up to level {1}. http://zero-k.info/Users/Detail/{2}",
                                              account.Name,
                                              account.Level,
                                              account.AccountID);
                            //text.AppendLine(message);
                            AuthServiceClient.SendLobbyMessage(account, message);
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError("Error sending level up lobby message: {0}", ex);
                        }
                    }
                }

                text.AppendLine(string.Format("BATTLE DETAILS AND REPLAY ----> http://zero-k.info/Battles/Detail/{0} <-----", sb.SpringBattleID));

                // create debriefing room, join players there and output message
                string channelName = "B" + sb.SpringBattleID;
                var joinplayers = new List<string>();
                joinplayers.AddRange(context.Players.Select(x => x.Name)); // add those who were there at start
                joinplayers.AddRange(sb.SpringBattlePlayers.Select(x => x.Account.Name)); // add those who played
                TasClient tas = Global.Nightwatch.Tas;
                Battle bat = tas.ExistingBattles.Values.FirstOrDefault(x => x.Founder.Name == context.AutohostName); // add those in lobby atm

                if (bat != null)
                {
                    List<string> inbatPlayers = bat.Users.Select(x => x.Name).ToList();
                    joinplayers.RemoveAll(x => inbatPlayers.Contains(x));
                }
                foreach (string jp in joinplayers.Distinct().Where(x => x != context.AutohostName)) tas.ForceJoinChannel(jp, channelName);
                tas.JoinChannel(channelName); // join nightwatch and say it
                tas.Say(TasClient.SayPlace.Channel, channelName, text.ToString(), true);
                tas.LeaveChannel(channelName);

                //text.Append(string.Format("Debriefing in #{0} - spring://chat/channel/{0}  ", channelName));
                return text.ToString();
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        static double GetEloDiff(SpringBattle sb)
        {
            double winnerElo = 0;
            double loserElo = 0;

            var losers = sb.SpringBattlePlayers.Where(x => !x.IsSpectator && !x.IsInVictoryTeam).Select(x => new { Player = x, x.Account }).ToList();
            var winners = sb.SpringBattlePlayers.Where(x => !x.IsSpectator && x.IsInVictoryTeam).Select(x => new { Player = x, x.Account }).ToList();

            if (losers.Count == 0 || winners.Count == 0)
            {
                return 1;
            }

            foreach (var r in winners)
            {
                winnerElo += r.Account.EffectiveElo;
            }
            foreach (var r in losers)
            {
                loserElo += r.Account.EffectiveElo;
            }

            winnerElo = winnerElo / winners.Count;
            loserElo = loserElo / losers.Count;

            return loserElo - winnerElo;
        }
    }
}