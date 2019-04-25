// Implementation of WHR " + category +" based on original by Pete Schwamb httpsin//github.com/goshrine/whole_history_rating

using Newtonsoft.Json;
using PlasmaShared;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using ZkData;

namespace Ratings
{

    public class WholeHistoryRating : IRatingSystem
    {

        public const float RatingOffset = 1500;
        public static readonly PlayerRating DefaultRating = new PlayerRating(int.MaxValue, 1, RatingOffset, float.PositiveInfinity, GlobalConst.NaturalRatingVariancePerDay(0), 0, 0, 1100, false);

        IDictionary<ITopPlayersUpdateListener, int> topPlayersUpdateListeners = new Dictionary<ITopPlayersUpdateListener, int>();
        public event EventHandler<RatingUpdate> RatingsUpdated;

        private float[] PercentileBrackets = { float.MinValue, 1200f, 1400f, 1600f, 1800f, 2000f, 2200f, 2400f, float.MaxValue };

        IDictionary<int, PlayerRating> playerOldRatings = new ConcurrentDictionary<int, PlayerRating>();
        IDictionary<int, PlayerRating> playerRatings = new ConcurrentDictionary<int, PlayerRating>();
        IDictionary<int, Player> players = new Dictionary<int, Player>();
        SortedDictionary<float, int> sortedPlayers = new SortedDictionary<float, int>();
        List<int> topPlayers = new List<int>();
        IDictionary<int, float> playerKeys = new Dictionary<int, float>();
        Random rand = new Random();
        private Timer ladderRecalculationTimer;
        private int activePlayers = 0;


        private int battlesRegistered = 0;
        private List<Account> laddersCache = new List<Account>();

        //private SpringBattle latestBattle, lastUpdate;
        private HashSet<int> ProcessedBattles = new HashSet<int>();
        private ConcurrentDictionary<int, PendingDebriefing> pendingDebriefings = new ConcurrentDictionary<int, PendingDebriefing>();
        private ConcurrentDictionary<int, PendingDebriefing> futureDebriefings = new ConcurrentDictionary<int, PendingDebriefing>();

        private bool completelyInitialized = false;

        private ConcurrentDictionary<int, PlayerRating> cachedDbRatings = new ConcurrentDictionary<int, PlayerRating>();

        private readonly RatingCategory category;

        public WholeHistoryRating(RatingCategory category)
        {
            this.category = category;
            ladderRecalculationTimer = new Timer((t) => { UpdateRatings(); }, this, 15 * 60000, (int)(GlobalConst.LadderUpdatePeriod * 3600 * 1000 + 4242));

        }

        public void ResetAll()
        {

            playerOldRatings = new ConcurrentDictionary<int, PlayerRating>();
            playerRatings = new ConcurrentDictionary<int, PlayerRating>();
            players = new Dictionary<int, Player>();
            sortedPlayers = new SortedDictionary<float, int>();
            topPlayers = new List<int>();
            playerKeys = new Dictionary<int, float>();
            activePlayers = 0;


            battlesRegistered = 0;
            laddersCache = new List<Account>();
            
            ProcessedBattles = new HashSet<int>();
            pendingDebriefings = new ConcurrentDictionary<int, PendingDebriefing>();
            futureDebriefings = new ConcurrentDictionary<int, PendingDebriefing>();
        }

        public PlayerRating GetPlayerRating(int accountID)
        {
            if (!completelyInitialized)
            {
                return cachedDbRatings.GetOrAdd(accountID,
                    id =>
                    {
                        using (var db = new ZkDataContext())
                            return db.AccountRatings.FirstOrDefault(x => x.AccountID == id && x.RatingCategory == category)
                                           ?.ToUnrankedPlayerRating() ?? DefaultRating;
                    });
            }
            
            return playerRatings.ContainsKey(RatingSystems.GetRatingId(accountID)) ? playerRatings[RatingSystems.GetRatingId(accountID)] : DefaultRating;
        }

        public Dictionary<DateTime, float> GetPlayerRatingHistory(int AccountID)
        {
            if (!players.ContainsKey(RatingSystems.GetRatingId(AccountID))) return new Dictionary<DateTime, float>();
            return players[RatingSystems.GetRatingId(AccountID)].days.ToDictionary(day => RatingSystems.ConvertDaysToDate(day.day), day => day.GetElo() + RatingOffset);
        }

        public Dictionary<DateTime, float> GetPlayerLadderRatingHistory(int AccountID)
        {
            if (!players.ContainsKey(RatingSystems.GetRatingId(AccountID))) return new Dictionary<DateTime, float>();
            return players[RatingSystems.GetRatingId(AccountID)].days.ToDictionary(day => RatingSystems.ConvertDaysToDate(day.day), day => day.GetElo() + RatingOffset - day.GetEloStdev() * GlobalConst.RatingConfidenceSigma);
        }

        public List<float> PredictOutcome(IEnumerable<IEnumerable<Account>> teams, DateTime time)
        {
            var predictions = teams.Select(t =>
                    SetupGame(t.Select(x => RatingSystems.GetRatingId(x.AccountID)).Distinct().ToList(),
                            teams.Where(t2 => !t2.Equals(t)).SelectMany(t2 => t2.Select(x => RatingSystems.GetRatingId(x.AccountID))).Distinct().ToList(),
                            true,
                            RatingSystems.ConvertDateToDays(time),
                            -1,
                            true
                    ).GetBlackWinProbability()).ToList();
            return predictions.Select(x => x / predictions.Sum()).ToList();
        }

        public void AttachResultReporting(int battleID, PendingDebriefing debriefing)
        {
            futureDebriefings.TryAdd(battleID, debriefing);
        }

        public void ProcessBattle(SpringBattle battle)
        {
            ICollection<int> winners = battle.SpringBattlePlayers.Where(p => p.IsInVictoryTeam && !p.IsSpectator).Select(p => RatingSystems.GetRatingId(p.AccountID)).Distinct().ToList();
            ICollection<int> losers = battle.SpringBattlePlayers.Where(p => !p.IsInVictoryTeam && !p.IsSpectator).Select(p => RatingSystems.GetRatingId(p.AccountID)).Distinct().ToList();

            int date = RatingSystems.ConvertDateToDays(battle.StartTime);

            if (RatingSystems.Initialized)
            {
                if (winners.Intersect(losers).Any()) Trace.TraceWarning("WHR B" + battle.SpringBattleID + " has winner loser intersection");
                if (ProcessedBattles.Contains(battle.SpringBattleID)) Trace.TraceWarning("WHR B" + battle.SpringBattleID + " has already been processed");
                if (winners.Count == 0) Trace.TraceWarning("WHR B" + battle.SpringBattleID + " has no winner");
                if (losers.Count == 0) Trace.TraceWarning("WHR B" + battle.SpringBattleID + " has no loser");
            }

            if (!winners.Intersect(losers).Any() && !ProcessedBattles.Contains(battle.SpringBattleID) && winners.Count > 0 && losers.Count > 0)
            {

                battlesRegistered++;
                ProcessedBattles.Add(battle.SpringBattleID);
                
                if (date > RatingSystems.ConvertDateToDays(DateTime.UtcNow))
                {
                    Trace.TraceWarning("WHR " + category + ": Tried to register battle " + battle.SpringBattleID + " which is from the future " + (date) + " > " + RatingSystems.ConvertDateToDays(DateTime.UtcNow));
                }
                else
                {

                    CreateGame(losers, winners, false, date, battle.SpringBattleID);
                    futureDebriefings.ForEach(u => pendingDebriefings.TryAdd(u.Key, u.Value));
                    futureDebriefings.Clear();
                 
                    if (RatingSystems.Initialized)
                    {
                        Trace.TraceInformation(battlesRegistered + " battles registered for WHR " + category + ", latest Battle: " + battle.SpringBattleID);
                        UpdateRatings();
                    }
                }
            }
            else
            {
                PendingDebriefing debriefing;
                futureDebriefings.TryGetValue(battle.SpringBattleID, out debriefing);
                if (debriefing == null) pendingDebriefings.TryGetValue(battle.SpringBattleID, out debriefing);
                if (debriefing != null)
                {
                    Trace.TraceWarning("Battle " + battle.SpringBattleID + " was processed before attaching pending report");
                    debriefing.debriefingConsumer.Invoke(debriefing.partialDebriefing);
                }
            }
        }


        public List<Account> GetTopPlayers(int count)
        {
            if (count > 200)
            {
                using (ZkDataContext db = new ZkDataContext())
                {
                    laddersCache = db.Accounts
                        .Include(a => a.Clan)
                        .Include(a => a.Faction)
                        .OrderByDescending(x => x.AccountRatings.Where(r => r.RatingCategory == category).Select(r => r.LadderElo).DefaultIfEmpty(-1).FirstOrDefault())
                        .Take(count)
                        .ToList();
                }
            }
            if (laddersCache.Count < count)
            {

                using (ZkDataContext db = new ZkDataContext())
                {
                    List<int> retIDs = topPlayers.Take(count).ToList();
                    laddersCache = db.Accounts
                        .Where(a => retIDs.Contains(a.AccountID))
                        .Include(a => a.Clan)
                        .Include(a => a.Faction)
                        .OrderByDescending(x => x.AccountRatings.Where(r => r.RatingCategory == category).Select(r => r.LadderElo).DefaultIfEmpty(-1).FirstOrDefault())
                        .ToList();
                }
            }
            return laddersCache.Take(count).ToList();
        }

        public List<Account> GetTopPlayers(int count, Func<Account, bool> selector)
        {
            lock (updateLockInternal)
            {
                int counter = 0;
                List<Account> retval = new List<Account>();

                using (ZkDataContext db = new ZkDataContext())
                {
                    foreach (var pair in sortedPlayers)
                    {
                        Account acc = db.Accounts
                            .Where(a => (a.AccountID) == pair.Value)
                            .Include(a => a.Clan)
                            .Include(a => a.Faction)
                            .FirstOrDefault();
                        if (playerRatings[RatingSystems.GetRatingId(acc.AccountID)].Rank < int.MaxValue && selector.Invoke(acc))
                        {
                            if (counter++ >= count) break;
                            retval.Add(acc);
                        }
                    }
                }
                return retval;
            }
        }

        //optimized selector method for high player counts
        public List<Account> GetTopPlayersIn(int count, Dictionary<int, Account> accounts)
        {
            lock (updateLockInternal)
            {
                int counter = 0;
                List<Account> retval = new List<Account>();

                Account acc;
                foreach (var pair in sortedPlayers)
                {
                    if (!accounts.ContainsKey(pair.Value)) continue;
                    acc = accounts[pair.Value];
                    if (playerRatings[RatingSystems.GetRatingId(acc.AccountID)].Rank < int.MaxValue)
                    {
                        if (counter++ >= count) break;
                        retval.Add(acc);
                    }
                }
                return retval;
            }
        }

        public void AddTopPlayerUpdateListener(ITopPlayersUpdateListener listener, int topX)
        {
            topPlayersUpdateListeners.Add(listener, topX);
        }

        public void RemoveTopPlayerUpdateListener(ITopPlayersUpdateListener listener, int topX)
        {
            topPlayersUpdateListeners.Remove(listener);
        }

        //implementation specific


        private DateTime lastUpdateTime;

        private readonly object updateLock = new object();
        private readonly static object updateLockInternal = new object();
        private readonly object dbLock = new object();

        public void ForceRatingsUpdate()
        {
            lastUpdateTime = DateTime.UtcNow.AddHours(-GlobalConst.LadderUpdatePeriod);
            UpdateRatings();
        }

        public void UpdateRatings()
        {
            if (!RatingSystems.Initialized) return;
            if (battlesRegistered == 0)
            {
                Trace.TraceWarning("No battles registered for WHR " + category);
                return;
            }
            lock (updateLock)
            {
                Action updateAction = null;
                if (!completelyInitialized)
                {
                    updateAction = (() =>
                    {
                        Trace.TraceInformation("Initializing WHR " + category + " ratings for " + battlesRegistered + " battles, this will take some time..");
                        runIterations(75);
                        UpdateRankings(players.Values);
                        playerOldRatings = new Dictionary<int, PlayerRating>(playerRatings);
                        completelyInitialized = true;
                        cachedDbRatings.Clear();
                    });
                }
                else if (DateTime.UtcNow.Subtract(lastUpdateTime).TotalHours >= GlobalConst.LadderUpdatePeriod)
                {
                    updateAction = (() =>
                    {
                        Trace.TraceInformation("Updating all WHR " + category + " ratings");
                        runIterations(1);
                        UpdateRankings(players.Values);
                    });
                    lastUpdateTime = DateTime.UtcNow;
                }
                else
                {
                    updateAction = (() =>
                    {
                        Trace.TraceInformation("Updating WHR " + category + " ratings for pending battles: " + pendingDebriefings.Keys.Select(x => "B" + x).StringJoin());
                        IEnumerable<Player> players = pendingDebriefings.Values.SelectMany(x => x.battle.SpringBattlePlayers).Where(p => !p.IsSpectator).Select(p => getPlayerById(RatingSystems.GetRatingId(p.AccountID)));
                        players.ForEach(p => p.RunOneNewtonIteration(true));
                        UpdateRankings(players);
                    });
                }
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        lock (updateLockInternal)
                        {
                            DateTime start = DateTime.Now;
                            updateAction.Invoke();
                            Trace.TraceInformation("WHR " + category + " Ratings updated in " + DateTime.Now.Subtract(start).TotalSeconds + " seconds, " + (GC.GetTotalMemory(false) / (1 << 20)) + "MiB total memory allocated");

                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Thread error while updating WHR " + category + " " + ex);
                    }
                }, CancellationToken.None, TaskCreationOptions.None, PriorityScheduler.BelowNormal);
                
            }

        }

        public RatingCategory GetRatingCategory()
        {
            return category;
        }
        public int GetActivePlayers()
        {
            return activePlayers;
        }

        public RankBracket GetPercentileBracket(int rank)
        {
            return new RankBracket()
            {
                LowerEloLimit = PercentileBrackets[Math.Min(PercentileBrackets.Length - 2, rank)],
                UpperEloLimit = PercentileBrackets[Math.Min(PercentileBrackets.Length - 1, rank + 1)],
            };
        }

        public void SaveToDB(IEnumerable<int> players)
        {
            lock (dbLock)
            {

                using (var db = new ZkDataContext())
                {
                    foreach (int player in players)
                    {
                        var accountRating = db.AccountRatings.Where(x => x.RatingCategory == category && (x.AccountID) == player).FirstOrDefault();
                        if (accountRating == null)
                        {
                            accountRating = new AccountRating(player, category);
                            accountRating.UpdateFromRatingSystem(playerRatings[player]);
                            db.AccountRatings.InsertOnSubmit(accountRating);
                        }
                        else
                        {
                            accountRating.UpdateFromRatingSystem(playerRatings[player]);
                        }
                    }
                    db.SaveChanges();
                }
            }
        }

        public void SaveToDB()
        {
            lock (dbLock)
            {
                DateTime start = DateTime.Now;
                using (var db = new ZkDataContext())
                {
                    HashSet<int> processedPlayers = new HashSet<int>();
                    int deleted = 0;
                    int added = 0;
                    foreach (var accountRating in db.AccountRatings.Where(x => x.RatingCategory == category))
                    {
                        if (!playerRatings.ContainsKey(accountRating.AccountID))
                        {
                            deleted++;
                            db.AccountRatings.DeleteOnSubmit(accountRating);
                            continue;
                        }
                        processedPlayers.Add(accountRating.AccountID);
                        if (Math.Abs(playerRatings[accountRating.AccountID].LadderElo - accountRating.LadderElo ?? 9999) > 1 || accountRating.IsRanked != (playerRatings[accountRating.AccountID].Rank < int.MaxValue))
                        {
                            accountRating.UpdateFromRatingSystem(playerRatings[accountRating.AccountID]);
                        }
                    }
                    foreach (int player in playerRatings.Keys)
                    {
                        if (!processedPlayers.Contains(player))
                        {
                            var accountRating = new AccountRating(player, category);
                            accountRating.UpdateFromRatingSystem(playerRatings[player]);
                            db.AccountRatings.InsertOnSubmit(accountRating);
                            added++;
                        }
                    }

                    db.SaveChanges();
                    Trace.TraceInformation("WHR " + category + " Ratings saved to DB in " + DateTime.Now.Subtract(start).TotalSeconds + " seconds, " + added + " entries added, " + deleted + " entries removed, " + (GC.GetTotalMemory(false) / (1 << 20)) + "MiB total memory allocated");
                }
            }
        }

        public string DebugPlayer(Account player)
        {
            if (!RatingSystems.Initialized) return "";
            if (!players.ContainsKey(RatingSystems.GetRatingId(player.AccountID))) return "Unknown player";
            string debugString = "";
            foreach (PlayerDay d in players[RatingSystems.GetRatingId(player.AccountID)].days)
            {
                debugString +=
                    d.day + ";" +
                    d.GetElo() + ";" +
                    d.naturalRatingVariance * 100 + ";" +
                    d.games[0].Select(g =>
                        g.whitePlayers.Select(p => p.id.ToString()).Aggregate("", (x, y) => x + "," + y) + "/" +
                        g.blackPlayers.Select(p => p.id.ToString()).Aggregate("", (x, y) => x + "," + y) + "/" +
                        (g.blackWins ? "Second" : "First") + "/" +
                        g.id
                    ).Aggregate("", (x, y) => x + "|" + y) + "\r\n";
            }
            return debugString;
        }

        //private


        //Runs in O(N log(N)) for all players
        private void UpdateRankings(IEnumerable<Player> players)
        {
            try
            {
                Dictionary<int, float> oldRatings = new Dictionary<int, float>();

                //check for ladder elo updates
                using (var db = new ZkDataContext())
                {
                    var battleIDs = pendingDebriefings.Keys.ToList();
                    var lastBattlePlayers = db.SpringBattlePlayers.Where(p => battleIDs.Contains(p.SpringBattleID) && !p.IsSpectator).Include(x => x.Account).ToList();
                    oldRatings = lastBattlePlayers.ToDictionary(p => p.AccountID, p => playerRatings[p.AccountID].LadderElo);
                    lastBattlePlayers.ForEach(p => playerRatings[p.AccountID].LadderElo = Ranks.UpdateLadderRating(p.Account, category, this.players[p.AccountID].avgElo + RatingOffset, p.IsInVictoryTeam, !p.IsInVictoryTeam, db));
                }

                //update ladders
                int currentDay = RatingSystems.ConvertDateToDays(DateTime.UtcNow);
                int playerCount = 0;
                using (var db = new ZkDataContext())
                {
                    foreach (var p in players)
                    {
                        if (p.days.Count == 0)
                        {
                            Trace.TraceError("WHR " + category + " has invalid player " + p.id + " with no days(games)");
                            continue;
                        }
                        float elo = p.days.Last().GetElo() + RatingOffset;
                        float lastNaturalRatingVar = p.avgEloVar * GlobalConst.EloToNaturalRatingMultiplierSquared;
                        var lastDay = p.days.Last();
                        float ladderElo;
                        if (playerRatings.ContainsKey(p.id)) ladderElo = playerRatings[p.id].LadderElo;
                        else ladderElo = (float?)db.AccountRatings.Where(x => x.AccountID == p.id && x.RatingCategory == category).FirstOrDefault()?.LadderElo ?? DefaultRating.LadderElo;
                        playerRatings[p.id] = new PlayerRating(int.MaxValue, 1, elo, lastNaturalRatingVar, GlobalConst.NaturalRatingVariancePerDay(lastDay.totalWeight), lastDay.day, currentDay, ladderElo, !float.IsNaN(p.avgElo));
                        float rating = -playerRatings[p.id].LadderElo + 0.001f * (float)rand.NextDouble();
                        if (playerKeys.ContainsKey(p.id)) sortedPlayers.Remove(playerKeys[p.id]);
                        playerKeys[p.id] = rating;
                        sortedPlayers[rating] = p.id;
                        if (playerRatings[p.id].Ranked) playerCount++;
                    }
                }
                this.activePlayers = playerCount;
                int rank = 0;
                List<int> newTopPlayers = new List<int>();
                int matched = 0;
                List<float> newPercentileBrackets = new List<float>();
                newPercentileBrackets.Add(playerRatings[sortedPlayers.First().Value].LadderElo + 420);
                float percentile;
                float[] percentilesRev = Ranks.Percentiles.Reverse().ToArray();
                foreach (var pair in sortedPlayers)
                {
                    if (playerRatings[pair.Value].Ranked)
                    {
                        newTopPlayers.Add(pair.Value);
                        if (rank == matched && rank < topPlayers.Count && topPlayers[rank] == pair.Value) matched++;
                        rank++;
                        percentile = (float)rank / activePlayers;
                        if (newPercentileBrackets.Count <= Ranks.Percentiles.Length && percentile > percentilesRev[newPercentileBrackets.Count - 1]) newPercentileBrackets.Add(playerRatings[pair.Value].LadderElo);
                        playerRatings[pair.Value].ApplyLadderUpdate(rank, percentile, currentDay, true);
                    }
                    else if (playerRatings[pair.Value].Rank < int.MaxValue)
                    {
                        playerRatings[pair.Value].ApplyLadderUpdate(int.MaxValue, 1, currentDay, false);
                    }
                }
                newPercentileBrackets.Add(newPercentileBrackets.Last() - 420);
                PercentileBrackets = newPercentileBrackets.Select(x => x).Reverse().ToArray();
                topPlayers = newTopPlayers;
                laddersCache = new List<Account>();
                Trace.TraceInformation("WHR " + category + " Ladders updated with " + topPlayers.Count + "/" + this.players.Count + " entries. Brackets are now: " + string.Join(", ", PercentileBrackets));

                var playerIds = players.Select(x => x.id).ToList();
                if (playerIds.Count() < 100)
                {
                    SaveToDB(playerIds);
                }
                else
                {
                    SaveToDB();
                }

                //check for rank updates

                if (pendingDebriefings.Any())
                {
                    List<int> playersWithRatingChange = new List<int>();
                    Dictionary<int, int> oldRanks = new Dictionary<int, int>();
                    Dictionary<int, Account> updatedRanks = new Dictionary<int, Account>();
                    Dictionary<int, Account> involvedAccounts = new Dictionary<int, Account>();
                    Trace.TraceInformation("WHR Filling in Debriefings for Battles: " + pendingDebriefings.Keys.Select(x => "B" + x).StringJoin());
                    using (var db = new ZkDataContext())
                    {
                        var battleIDs = pendingDebriefings.Keys.ToList();
                        var lastBattlePlayers = db.SpringBattlePlayers.Where(p => battleIDs.Contains(p.SpringBattleID) && !p.IsSpectator).Include(x => x.Account).ToList();
                        involvedAccounts = lastBattlePlayers.ToDictionary(p => p.AccountID, p => p.Account);
                        Trace.TraceInformation("WHR Debriefing players: " + involvedAccounts.Values.Select(x => x.Name).StringJoin());
                        oldRanks = lastBattlePlayers.ToDictionary(p => p.AccountID, p => p.Account.Rank);
                        updatedRanks = lastBattlePlayers.Where(p => Ranks.UpdateRank(p.Account, p.IsInVictoryTeam, !p.IsInVictoryTeam, db)).Select(x => x.Account).ToDictionary(p => p.AccountID, p => p);
                        updatedRanks.Values.ForEach(p => db.Entry(p).State = EntityState.Modified);
                        playersWithRatingChange = lastBattlePlayers.Select(x => x.AccountID).ToList();

                        lastBattlePlayers.Where(p => playerOldRatings.ContainsKey(RatingSystems.GetRatingId(p.AccountID)) && !p.EloChange.HasValue).ForEach(p =>
                        {
                            //p.EloChange = playerRatings[RatingSystems.GetRatingId(p.AccountID)].RealElo - playerOldRatings[RatingSystems.GetRatingId(p.AccountID)].RealElo;
                            p.EloChange = playerRatings[p.AccountID].LadderElo - oldRatings[p.AccountID];
                        });

                        db.SpringBattlePlayers.Where(p => battleIDs.Contains(p.SpringBattleID) && !p.IsSpectator).ToList().ForEach(x => playerOldRatings[RatingSystems.GetRatingId(x.AccountID)] = playerRatings[RatingSystems.GetRatingId(x.AccountID)]);
                        db.SaveChanges();
                    }
                    //Publish new results only after saving new stats to db.
                    pendingDebriefings.ForEach(pair =>
                    {
                        pair.Value.partialDebriefing.DebriefingUsers.Values.ForEach(user => {
                            try
                            {
                                user.EloChange = playerRatings[user.AccountID].LadderElo - oldRatings[user.AccountID];
                                user.IsRankup = updatedRanks.ContainsKey(user.AccountID) && oldRanks[user.AccountID] < updatedRanks[user.AccountID].Rank;
                                user.IsRankdown = updatedRanks.ContainsKey(user.AccountID) && oldRanks[user.AccountID] > updatedRanks[user.AccountID].Rank;
                                var prog = Ranks.GetRankProgress(involvedAccounts[user.AccountID], this);
                                if (prog == null) Trace.TraceWarning("User " + user.AccountID + " is wrongfully unranked");
                                user.NextRankElo = prog.RankCeilElo;
                                user.PrevRankElo = prog.RankFloorElo;
                                user.NewElo = prog.CurrentElo;
                            }
                            catch(Exception ex)
                            {
                                Trace.TraceError("Unable to complete debriefing for user " + user.AccountID + ": " + ex);
                            }
                        });
                        pair.Value.partialDebriefing.RatingCategory = category.ToString();
                        pair.Value.debriefingConsumer.Invoke(pair.Value.partialDebriefing);
                    });
                    RatingsUpdated(this, new RatingUpdate() { affectedPlayers = playersWithRatingChange });
                    pendingDebriefings.Clear();
                }
                

                //check for topX updates
                GetTopPlayers(GlobalConst.LadderSize);
                foreach (var listener in topPlayersUpdateListeners)
                {
                    if (matched < listener.Value)
                    {
                        listener.Key.TopPlayersUpdated(GetTopPlayers(listener.Value));
                    }
                }

            }
            catch (Exception ex)
            {
                string dbg = "WHR " + category + ": Failed to update rankings " + ex + "\nPlayers: ";
                foreach (var p in players)
                {
                    dbg += p.id + " (" + p.days.Count + " days), ";
                }
                Trace.TraceError(dbg);
            }
        }


        private Player getPlayerById(int id, bool temporary = false)
        {
            if (!players.ContainsKey(id))
            {
                if (temporary) return new Player(id);
                lock (updateLockInternal)
                {
                    players.Add(id, new Player(id));
                }
            }
            return players[id];
        }

        private Game SetupGame(ICollection<int> black, ICollection<int> white, bool blackWins, int time_step, int id, bool temporary)
        {

            // Avoid self-played games (no info)
            if (black.Equals(white))
            {
                Trace.TraceError("White == Black");
                return null;
            }
            if (white.Count < 1)
            {
                Trace.TraceError("White empty");
                return null;
            }
            if (black.Count < 1)
            {
                Trace.TraceError("Black empty");
                return null;
            }


            List<Player> white_player = white.Select(p => getPlayerById(p, temporary)).ToList();
            List<Player> black_player = black.Select(p => getPlayerById(p, temporary)).ToList();
            Game game = new Game(black_player, white_player, blackWins, time_step, id);
            return game;
        }

        private Game CreateGame(ICollection<int> black, ICollection<int> white, bool blackWins, int time_step, int id)
        {
            Game game = SetupGame(black, white, blackWins, time_step, id, false);
            return game != null ? AddGame(game) : null;
        }

        private Game AddGame(Game game)
        {
            game.whitePlayers.ForEach(p => p.AddGame(game));
            game.blackPlayers.ForEach(p => p.AddGame(game));
            
            return game;
        }

        private void runIterations(int count)
        {
            for (int i = 0; i < count - 1; i++)
            {
                players.Values.ForEach(x => x.RunOneNewtonIteration(false));
            }
            players.Values.ForEach(x => x.RunOneNewtonIteration(true));
        }

        private void printStats()
        {
            float sum = 0;
            int bigger = 0;
            int total = 0;
            float lowest = 0;
            float highest = 0;
            foreach (Player p in players.Values)
            {
                if (p.days.Count > 0)
                {
                    total++;
                    float elo = p.days[p.days.Count - 1].GetElo();
                    sum += elo;
                    if (elo > 0) bigger++;
                    lowest = Math.Min(lowest, elo);
                    highest = Math.Max(highest, elo);
                }
            }
            Trace.TraceInformation("Lowest eloin " + lowest);
            Trace.TraceInformation("Highest eloin " + highest);
            Trace.TraceInformation("sum eloin " + sum);
            Trace.TraceInformation("Average eloin " + (sum / total));
            Trace.TraceInformation("Amount > 0in " + bigger);
            Trace.TraceInformation("Amount < 0in " + (total - bigger));
        }
    }

    public class RatingUpdate : EventArgs
    {
        public IEnumerable<int> affectedPlayers { get; set; }
    }

}
