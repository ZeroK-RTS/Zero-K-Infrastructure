using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using LobbyClient;
using Newtonsoft.Json;
using PlasmaShared;
using ZkData;
using Ratings;
using ZkLobbyServer;

namespace ZeroKWeb.SpringieInterface
{
    /// <summary>
    /// Handle everything to do with battle results: change Elo and XP, awards, PlanetWars effects...
    /// </summary>
    public class BattleResultHandler
    {
        //stores replay, springbattle, processes rating and xp, returns whether battle ended normally
        public static bool SubmitSpringBattleResult(SpringBattleContext result, ZkLobbyServer.ZkLobbyServer server, Action<BattleDebriefing> consumer)
        {
            var ret = new BattleDebriefing();
            try
            {
                if (string.IsNullOrEmpty(result.ReplayName))
                {
                    ret.Message = "Game didn't start";
                    return false;
                }

                bool isValidGame = true;
                if (!result.GameEndedOk)
                {
                    ret.Message = "Game didn't end properly";
                    isValidGame = false;
                }
                if (result.IsCheating)
                {
                    ret.Message = "Cheats were enabled during this game";
                    isValidGame = false;
                }

                var db = new ZkDataContext();
                var text = new StringBuilder();

                var sb = SaveSpringBattle(result, db);

                // find and upload replay (async)
                ReplayStorage.Instance.UploadAndDeleteFileAsync(Path.Combine(server.SpringPaths.WritableDirectory,"demos-server", sb.ReplayFileName));
                // store infolog
                ReplayStorage.Instance.UploadAndDeleteFileAsync(Path.Combine(server.SpringPaths.WritableDirectory, $"infolog_{sb.EngineGameID}.txt"));

                if (isValidGame)
                {
                    StoreAwards(result.OutputExtras, sb, db);
                }
                StoreLogs(result.OutputExtras, sb, db);

                if (result.LobbyStartContext.Mode == AutohostMode.Planetwars) ProcessPlanetWars(result, server, sb, db, text);


                Dictionary<int, int> orgLevels = sb.SpringBattlePlayers.Select(x => x.Account).ToDictionary(x => x.AccountID, x => x.Level);

                // check for noelo
                if (!isValidGame || result.LobbyStartContext.ModOptions.Any(x => x.Key.ToLower() == "noelo" && x.Value != "0" && x.Value != "false"))
                {
                    sb.ApplicableRatings = 0;
                }
                else
                {
                    RatingSystems.FillApplicableRatings(sb, result);
                }

                if (isValidGame)
                {
                    ProcessXP(result, server, db, sb);
                }

                
                ret.Url = string.Format("{1}/Battles/Detail/{0}", sb.SpringBattleID, GlobalConst.BaseSiteUrl);
                ret.ServerBattleID = sb.SpringBattleID;

                server.GhostSay(
                    new Say()
                    {
                        Text =
                            string.Format("BATTLE DETAILS AND REPLAY ----> {0} <-----", ret.Url),
                        IsEmote = true,
                        Place = SayPlace.Battle,
                        User = GlobalConst.NightwatchName
                    },
                    result.LobbyStartContext.BattleID);


                foreach (var p in sb.SpringBattlePlayers.Where(x=>!x.IsSpectator))
                {
                    ret.DebriefingUsers[p.Account.Name] = new BattleDebriefing.DebriefingUser()
                    {
                        AccountID = p.AccountID,
                        LoseTime = p.LoseTime ?? -1,
                        AllyNumber = p.AllyNumber,
                        IsInVictoryTeam = p.IsInVictoryTeam,
                        EloChange = 0,
                        IsRankdown = false,
                        IsRankup = false,
                        NewElo = -1,
                        NextRankElo = -1,
                        PrevRankElo = -1,
                        NewRank = p.Account.Rank,
                        XpChange = p.XpChange ?? 0,
                        NewXp = p.Account.Xp,
                        NextLevelXp = Account.GetXpForLevel(p.Account.Level + 1),
                        PrevLevelXp = Account.GetXpForLevel(p.Account.Level),
                        IsLevelUp = orgLevels[p.AccountID] < p.Account.Level,
                        Awards = sb.AccountBattleAwards.Where(x=>x.AccountID == p.AccountID).Select(x=> new BattleDebriefing.DebriefingAward()
                        {
                            Value = x.Value,
                            Key = x.AwardKey,
                            Description = x.AwardDescription
                        }).ToList()
                    };
                }

                //send to rating
                RatingSystems.ProcessResult(sb, result, new PendingDebriefing()
                {
                    debriefingConsumer = consumer,
                    partialDebriefing = ret,
                    battle = sb,
                });

                Trace.TraceInformation("Battle ended: Server exited for B" + sb.SpringBattleID);
                db.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {
                var data = JsonConvert.SerializeObject(result);
                Trace.TraceError($"{ex}\nData:\n{data}");
                ret.Message = "Error processing game result: " + ex.Message;
                return false;
            }
        }

        private static void ProcessXP(SpringBattleContext result, ZkLobbyServer.ZkLobbyServer server, ZkDataContext db, SpringBattle sb)
        {

            sb.DispenseXP();

            foreach (var u in sb.SpringBattlePlayers.Where(x => !x.IsSpectator)) u.Account.CheckLevelUp();


            db.SaveChanges();

            if (sb.ApplicableRatings == 0)
            {
                try
                {
                    foreach (Account a in sb.SpringBattlePlayers.Where(x => !x.IsSpectator).Select(x => x.Account))
                    {
                        server.PublishAccountUpdate(a);
                        server.PublishUserProfileUpdate(a);
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError("error updating extension data: {0}", ex);
                }
            }
        }

        private static void ProcessPlanetWars(SpringBattleContext result, ZkLobbyServer.ZkLobbyServer server, SpringBattle sb, ZkDataContext db, StringBuilder text)
        {
            if (result.LobbyStartContext.Mode != AutohostMode.Planetwars || sb.PlayerCount < 2 || sb.Duration < GlobalConst.MinDurationForPlanetwars || sb.Duration > GlobalConst.MaxDurationForPlanetwars) return;

            List<int> winnerTeams = sb.SpringBattlePlayers.Where(x => x.IsInVictoryTeam && !x.IsSpectator).Select(x => x.AllyNumber).Distinct().ToList();
            int? winNum = null;
            if (winnerTeams.Count == 1)
            {
                winNum = winnerTeams[0];
                if (winNum > 1) winNum = null;
            }

            PlanetWarsTurnHandler.EndTurn(result.LobbyStartContext.Map,
                result.OutputExtras,
                db,
                winNum,
                sb.SpringBattlePlayers.Where(x => !x.IsSpectator).Select(x => x.Account).ToList(),
                text,
                sb,
                sb.SpringBattlePlayers.Where(x => !x.IsSpectator && x.AllyNumber == 0).Select(x => x.Account).ToList(),
                server.PlanetWarsEventCreator, server);

            server.PlanetWarsMatchMaker.RemoveFromRunningBattles(result.LobbyStartContext.BattleID);
        }

        private static SpringBattle SaveSpringBattle(SpringBattleContext result, ZkDataContext db)
        {
            var sb = new SpringBattle
            {
                HostAccountID = Account.AccountByName(db, result.LobbyStartContext.FounderName)?.AccountID,
                Mode = result.LobbyStartContext.Mode,
                Duration = result.Duration,
                EngineGameID = result.EngineBattleID,
                MapResourceID = db.Resources.Single(x => x.InternalName == result.LobbyStartContext.Map).ResourceID,
                ModResourceID = db.Resources.Single(x => x.InternalName == result.LobbyStartContext.Mod).ResourceID,
                HasBots = result.LobbyStartContext.Bots.Any(),
                IsMission = result.LobbyStartContext.IsMission,
                PlayerCount = result.ActualPlayers.Count(x => !x.IsSpectator),
                StartTime = result.StartTime,
                Title = result.LobbyStartContext.Title,
                ReplayFileName = Path.GetFileName(result.ReplayName),
                EngineVersion = result.LobbyStartContext.EngineVersion,
                IsMatchMaker = result.LobbyStartContext.IsMatchMakerGame,
                ApplicableRatings = 0,
            };
            db.SpringBattles.InsertOnSubmit(sb);

            // store players
            foreach (BattlePlayerResult p in result.ActualPlayers)
            {
                var account = Account.AccountByName(db, p.Name);
                if (account != null)
                {
                    sb.SpringBattlePlayers.Add(new SpringBattlePlayer
                    {
                        Account = account,
                        AccountID = account.AccountID,
                        AllyNumber = p.AllyNumber,
                        IsInVictoryTeam = p.IsVictoryTeam,
                        IsSpectator = p.IsSpectator,
                        LoseTime = p.LoseTime
                    });
                }
            }

            // store bots
            var victoryAllyID = result.ActualPlayers.Where(x => x.IsVictoryTeam).Select(x => (int?)x.AllyNumber).FirstOrDefault() ?? -1;
            if (victoryAllyID == -1) victoryAllyID = (result.ActualPlayers.Min(x => (int?)x.AllyNumber)??-1) + 1; // no player won, its likely to be next lowes team (stupid hack needed)
            foreach (var bot in result.LobbyStartContext.Bots)
            {
                sb.SpringBattleBots.Add(new SpringBattleBot()
                {
                    AllyNumber = bot.AllyID,
                    BotAI = bot.BotAI,
                    BotName = bot.BotName,
                    IsInVictoryTeam = bot.AllyID == victoryAllyID
                });
            }

            db.SaveChanges();

            return db.SpringBattles.FirstOrDefault(x => x.SpringBattleID == sb.SpringBattleID); // reselect from db to get proper lazy proxies
        }

        private static void StoreAwards(List<string> extras, SpringBattle sb, ZkDataContext db)
        {
            foreach (string line in extras.Where(x => x?.StartsWith("award") == true))
            {
                string[] partsSpace = line.Substring(6).Split(new[] { ' ' }, 3);
                string name = partsSpace[0];
                string awardType = partsSpace[1];
                string awardText = partsSpace[2];

                // prevent hax: tourney cups and event coins are never given automatically
                if (awardType != null && (awardType == "gold" || awardType == "silver" || awardType == "bronze")) continue;
                if (awardType != null && (awardType == "goldcoin" || awardType == "silvercoin" || awardType == "bronzecoin")) continue;

                SpringBattlePlayer player = sb.SpringBattlePlayers.FirstOrDefault(x => x.Account?.Name == name);
                if (player != null)
                {
                    db.AccountBattleAwards.InsertOnSubmit(new AccountBattleAward
                    {
                        Account = player.Account,
                        AccountID = player.AccountID,
                        SpringBattleID = sb.SpringBattleID,
                        AwardKey = awardType,
                        AwardDescription = awardText
                    });
                }
            }
        }
        private static void StoreLogs(List<string> extras, SpringBattle sb, ZkDataContext db)
        {
            // chatlogs
            foreach (string line in extras.Where(x => x?.StartsWith("CHATLOG") == true))
            {
                string[] partsSpace = line.Substring(8).Split(new[] { ' ' }, 2);
                string name = partsSpace[0];
                string chatlog = partsSpace[1];

                SpringBattlePlayer player = sb.SpringBattlePlayers.FirstOrDefault(x => x.Account?.Name == name);
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
        }
    }
}
