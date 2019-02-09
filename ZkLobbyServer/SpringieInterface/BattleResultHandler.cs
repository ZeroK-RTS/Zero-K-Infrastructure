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

namespace ZeroKWeb.SpringieInterface
{
    /// <summary>
    /// Handle everything to do with battle results: change Elo and XP, awards, PlanetWars effects...
    /// </summary>
    public class BattleResultHandler
    {
        public static BattleDebriefing SubmitSpringBattleResult(SpringBattleContext result, ZkLobbyServer.ZkLobbyServer server)
        {
            var ret = new BattleDebriefing();
            try
            {
                if (!result.GameEndedOk)
                {
                    ret.Message = "Game didn't end properly";
                    return ret;
                }
                if (result.IsCheating)
                {
                    ret.Message = "Cheats were enabled during this game";
                    return ret;
                }

                var db = new ZkDataContext();
                var text = new StringBuilder();

                var sb = SaveSpringBattle(result, db);



                ProcessExtras(result.OutputExtras, sb, db);

                if (result.LobbyStartContext.Mode == AutohostMode.Planetwars) ProcessPlanetWars(result, server, sb, db, text);


                Dictionary<int, int> orgLevels = sb.SpringBattlePlayers.Select(x => x.Account).ToDictionary(x => x.AccountID, x => x.Level);

                ProcessRatingAndXP(result, server, db, sb);

                
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
                        LoseTime = p.LoseTime,
                        AllyNumber = p.AllyNumber,
                        IsInVictoryTeam = p.IsInVictoryTeam,
                        EloChange = p.EloChange?.ToString("F2"),
                        XpChange = p.XpChange,
                        IsLevelUp = orgLevels[p.AccountID] < p.Account.Level,
                        Awards = sb.AccountBattleAwards.Where(x=>x.AccountID == p.AccountID).Select(x=> new BattleDebriefing.DebriefingAward()
                        {
                            Value = x.Value,
                            Key = x.AwardKey,
                            Description = x.AwardDescription
                        }).ToList()
                    };
                }

                return ret;
            }
            catch (Exception ex)
            {
                var data = JsonConvert.SerializeObject(result);
                Trace.TraceError($"{ex}\nData:\n{data}");
                ret.Message = "Error processing game result: " + ex.Message;
                return ret;
            }
        }

        private static void ProcessRatingAndXP(SpringBattleContext result, ZkLobbyServer.ZkLobbyServer server, ZkDataContext db, SpringBattle sb)
        {
            bool noElo = result.OutputExtras.Any(x => x?.StartsWith("noElo", true, System.Globalization.CultureInfo.CurrentCulture) == true);


            if (!noElo) RatingSystems.ProcessResult(sb, result);

            sb.DispenseXP();

            foreach (var u in sb.SpringBattlePlayers.Where(x => !x.IsSpectator)) u.Account.CheckLevelUp();


            db.SaveChanges();

            /* Ideally, updating the ratings would also take care of updating level:
             *    if (sb.ApplicableRatings == 0 || noElo)
             * However, sometimes games that are nominally ranked will not actually
             * touch ratings, leading to level being left unupdated. This can happen
             * both as a result of the bug where the server never learns a team died,
             * leading to games with both teams winning or a very rare but legitimate
             * game draw (eg. when two crawling bombs are left and blow each other),
             * which the server interprets as a game with two losers. Ideally the
             * rating system would handle draws and the bug would be fixed but for now
             * profile updates are sent unconditionally to make sure levels are correct. */
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

        private static void ProcessExtras(List<string> extras, SpringBattle sb, ZkDataContext db)
        {
            // awards
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
