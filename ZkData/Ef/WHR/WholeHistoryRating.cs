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
            accountID = (accountID);
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

            return playerRatings.ContainsKey(accountID) ? playerRatings[accountID] : DefaultRating;
        }

        public Dictionary<DateTime, float> GetPlayerRatingHistory(int AccountID)
        {
            if (!players.ContainsKey((AccountID))) return new Dictionary<DateTime, float>();
            return players[(AccountID)].days.ToDictionary(day => RatingSystems.ConvertDaysToDate(day.day), day => day.GetElo() + RatingOffset);
        }
        public float GetAverageRecentWinChance(int AccountID)
        {
            if (!players.ContainsKey((AccountID))) return 0.4f;
            var recentGames = players[AccountID].days
                .Where(day => day.day >= RatingSystems.ConvertDateToDays(DateTime.UtcNow.AddDays(-1)))
                .SelectMany(day => day.games.SelectMany(g => g))
                .OrderByDescending(g => g.id)
                .Take(5);
            float recentWinChance = 0.4f;
            if (recentGames.Count() > 0)
            {
                recentWinChance = recentGames.Select(x => x.winnerPlayers.Contains(players[AccountID]) ? x.GetWinProbability() : (1 - x.GetWinProbability())).Average();
            }
            //Trace.TraceInformation($"The {recentGames.Count()} most recent games for {AccountID} are {string.Join(", ", recentGames.Select(x => x.id))}, the average win chance is {recentWinChance}.");
            return recentWinChance;
        }

        public List<float> PredictOutcome(IEnumerable<IEnumerable<Account>> teams, DateTime time)
        {
            var predictions = teams.Select(t =>
                    SetupGame(t.Select(x => (x.AccountID)).Distinct().ToList(),
                            teams.Where(t2 => !t2.Equals(t)).Select(t2 => (ICollection<int>)t2.Select(x => (x.AccountID)).Distinct().ToList()).ToList(),
                            RatingSystems.ConvertDateToDays(time),
                            -1,
                            true
                    ).GetWinProbability()).ToList();
            return predictions;
        }

        public void AttachResultReporting(int battleID, PendingDebriefing debriefing)
        {
            futureDebriefings.TryAdd(battleID, debriefing);
        }

        public void ProcessBattle(SpringBattle battle)
        {
            ICollection<int> winners = battle.SpringBattlePlayers
                .Where(p => p.IsInVictoryTeam && !p.IsSpectator)
                .Select(p => (p.AccountID))
                .Distinct()
                .ToList();
            ICollection<ICollection<int>> losers = battle.SpringBattlePlayers
                .Where(p => !p.IsInVictoryTeam && !p.IsSpectator)
                .GroupBy(p => p.AllyNumber)
                .Select(t => (ICollection<int>)t.Select(p => p.AccountID).Distinct().ToList())
                .ToList();

            int date = RatingSystems.ConvertDateToDays(battle.StartTime);

            if (RatingSystems.Initialized)
            {
                if (losers.Any(t => winners.Intersect(t).Any())) Trace.TraceWarning("WHR B" + battle.SpringBattleID + " has winner loser intersection");
                if (ProcessedBattles.Contains(battle.SpringBattleID)) Trace.TraceWarning("WHR B" + battle.SpringBattleID + " has already been processed");
                if (winners.Count == 0) Trace.TraceWarning("WHR B" + battle.SpringBattleID + " has no winner");
                if (losers.Count == 0) Trace.TraceWarning("WHR B" + battle.SpringBattleID + " has no loser");
            }

            if (!losers.Any(t => winners.Intersect(t).Any()) && !ProcessedBattles.Contains(battle.SpringBattleID) && winners.Count > 0 && losers.Count > 0)
            {

                battlesRegistered++;
                ProcessedBattles.Add(battle.SpringBattleID);

                if (date > RatingSystems.ConvertDateToDays(DateTime.UtcNow))
                {
                    Trace.TraceWarning("WHR " + category + ": Tried to register battle " + battle.SpringBattleID + " which is from the future " + (date) + " > " + RatingSystems.ConvertDateToDays(DateTime.UtcNow));
                }
                else
                {

                    CreateGame(winners, losers, date, battle.SpringBattleID);
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
                        if (playerRatings[pair.Value].Rank < int.MaxValue && selector.Invoke(acc))
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
                    if (playerRatings[pair.Value].Rank < int.MaxValue)
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
                        IEnumerable<Player> players = pendingDebriefings.Values.SelectMany(x => x.battle.SpringBattlePlayers).Where(p => !p.IsSpectator).Select(p => getPlayerById((p.AccountID)));
                        players.ForEach(p => p.RunOneNewtonIteration(true));
                        UpdateRankings(this.players.Values);
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
                        if (Math.Abs(playerRatings[accountRating.AccountID].LadderElo - accountRating.LadderElo ?? 9999) > 0.5
                            || Math.Abs(playerRatings[accountRating.AccountID].RealElo - accountRating.RealElo) > 0.5
                            || accountRating.IsRanked != (playerRatings[accountRating.AccountID].Rank < int.MaxValue))
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
            if (!players.ContainsKey((player.AccountID))) return "Unknown player";
            string debugString = "";
            foreach (PlayerDay d in players[(player.AccountID)].days)
            {
                debugString +=
                    d.day + ";" +
                    d.GetElo() + ";" +
                    d.naturalRatingVariance * 100 + ";" +
                    d.games[0].Select(g =>
                        g.loserPlayers.Select(t => t.Select(p => p.id.ToString()).Aggregate("", (x, y) => x + "," + y)).Aggregate("", (x, y) => x + "/" + y) + "/" +
                        g.winnerPlayers.Select(p => p.id.ToString()).Aggregate("", (x, y) => x + "," + y) + "/" +
                        ("Last is winner") + "/" +
                        g.id
                    ).Aggregate("", (x, y) => x + "|" + y) + "\r\n";
            }
            return debugString;
        }

        //private


        //Runs in O(N log(N)) for all players
        private void UpdateRankings(IEnumerable<Player> players)
        {
            var debriefings = new Dictionary<int, PendingDebriefing>(pendingDebriefings);
            int matched = 0;

            try
            {

                //check for ladder elo updates
                using (var db = new ZkDataContext())
                {
                    var battleIDs = debriefings.Keys.ToList();
                    foreach (var battleId in debriefings.Keys)
                    {
                        List<SpringBattlePlayer> lastBattlePlayers = db.SpringBattlePlayers.Where(p => p.SpringBattleID == battleId && !p.IsSpectator).Include(x => x.Account).DistinctBy(x => x.AccountID).ToList();
                        Dictionary<int, float> oldRatings = lastBattlePlayers.ToDictionary(p => (p.AccountID), p => GetPlayerRating(p.AccountID).LadderElo);
                        lastBattlePlayers.Where(p => !playerRatings.ContainsKey((p.AccountID))).ForEach(p => playerRatings[(p.AccountID)] = new PlayerRating(DefaultRating));
                        Dictionary<int, float> winChances = db.SpringBattles.Where(p => p.SpringBattleID == battleId).First().GetAllyteamWinChances();
                        lastBattlePlayers.ForEach(p => {
                            float eloChange = (p.IsInVictoryTeam ? (1f - winChances[p.AllyNumber]) : (-winChances[p.AllyNumber])) * GlobalConst.LadderEloClassicEloK / lastBattlePlayers.Count(x => x.AllyNumber == p.AllyNumber);
                            playerRatings[p.AccountID].LadderElo = Ranks.UpdateLadderRating(p.Account, category, getPlayerById(p.AccountID).avgElo + RatingOffset, p.IsInVictoryTeam, !p.IsInVictoryTeam, eloChange, db);
                        });
                        lastBattlePlayers.Where(p => !p.EloChange.HasValue).ForEach(p =>
                        {
                            p.EloChange = playerRatings[(p.AccountID)].LadderElo - oldRatings[(p.AccountID)];
                            db.SpringBattlePlayers.Attach(p);
                            db.Entry(p).Property(x => x.EloChange).IsModified = true;
                        });
                        db.SaveChanges();
                    }
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
                        float lastNaturalRatingVar = p.days.Last().naturalRatingVariance;
                        var lastDay = p.days.Last();
                        float ladderElo;
                        if (playerRatings.ContainsKey(p.id)) ladderElo = playerRatings[p.id].LadderElo;
                        else ladderElo = (float?)db.AccountRatings.Where(x => x.AccountID == p.id && x.RatingCategory == category).FirstOrDefault()?.LadderElo ?? DefaultRating.LadderElo;
                        playerRatings[p.id] = new PlayerRating(int.MaxValue, 1, elo, lastNaturalRatingVar, GlobalConst.NaturalRatingVariancePerDay(lastDay.totalWeight), lastDay.day, currentDay, ladderElo, !float.IsNaN(p.avgElo));
                        float rating = -playerRatings[p.id].LadderElo;
                        if (playerKeys.ContainsKey(p.id)) sortedPlayers.Remove(playerKeys[p.id]);
                        while (sortedPlayers.ContainsKey(rating)) rating += 0.01f;
                        playerKeys[p.id] = rating;
                        sortedPlayers[rating] = p.id;
                        if (playerRatings[p.id].Ranked) playerCount++;
                    }
                }
                this.activePlayers = playerCount;
                int rank = 0;
                List<int> newTopPlayers = new List<int>();
                List<float> newPercentileBrackets = new List<float>();
                newPercentileBrackets.Add(playerRatings[sortedPlayers.First().Value].LadderElo);
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
                if (rank != playerCount) Trace.TraceWarning("WHR has " + playerCount + " active players, but " + rank + " sorted active players");
                while (newPercentileBrackets.Count < Ranks.Percentiles.Length + 1) newPercentileBrackets.Add(playerRatings[sortedPlayers.Last().Value].LadderElo);
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

            }
            catch (Exception ex)
            {
                Trace.TraceError("WHR " + category + ": Failed to update rankings: " + ex);
                PendingDebriefing discard2;
                debriefings.ForEach(x => pendingDebriefings.TryRemove(x.Key, out discard2));
                return;
            }
            try
            {
                //check for rank updates
                if (debriefings.Any())
                {
                    Trace.TraceInformation("WHR Filling in Debriefings for Battles: " + debriefings.Keys.Select(x => "B" + x).StringJoin());
                    using (var db = new ZkDataContext())
                    {

                        foreach (var battleId in debriefings.Keys)
                        {
                            List<SpringBattlePlayer> lastBattlePlayers = db.SpringBattlePlayers.Where(p => p.SpringBattleID == battleId && !p.IsSpectator).Include(x => x.Account).DistinctBy(x => x.AccountID).ToList();
                            Dictionary<int, SpringBattlePlayer> involvedPlayers = lastBattlePlayers.ToDictionary(p => p.AccountID, p => p);
                            Trace.TraceInformation("WHR Debriefing players: " + involvedPlayers.Values.Select(x => x.Account.Name).StringJoin());
                            Dictionary<int, int> oldRanks = lastBattlePlayers.ToDictionary(p => p.AccountID, p => p.Account.Rank);
                            Dictionary<int, Account> updatedRanks = lastBattlePlayers.Where(p => Ranks.UpdateRank(p.Account, p.IsInVictoryTeam, !p.IsInVictoryTeam, db)).Select(x => x.Account).ToDictionary(p => p.AccountID, p => p);
                            updatedRanks.Values.ForEach(p =>
                            {
                                db.Accounts.Attach(p);
                                db.Entry(p).Property(x => x.Rank).IsModified = true;
                            });
                            List<int> playersWithRatingChange = lastBattlePlayers.Select(x => x.AccountID).ToList();
                            db.SaveChanges();

                            //Publish new results only after saving new stats to db.
                            debriefings[battleId].partialDebriefing.DebriefingUsers.Values.ForEach(user =>
                            {
                                try
                                {
                                    user.EloChange = involvedPlayers[user.AccountID].EloChange ?? 0;
                                    user.IsRankup = updatedRanks.ContainsKey(user.AccountID) && oldRanks[user.AccountID] < updatedRanks[user.AccountID].Rank;
                                    user.IsRankdown = updatedRanks.ContainsKey(user.AccountID) && oldRanks[user.AccountID] > updatedRanks[user.AccountID].Rank;
                                    var prog = Ranks.GetRankProgress(involvedPlayers[user.AccountID].Account, this);
                                    if (prog == null) Trace.TraceWarning("User " + user.AccountID + " is wrongfully unranked");
                                    user.NextRankElo = prog.RankCeilElo;
                                    user.PrevRankElo = prog.RankFloorElo;
                                    user.NewElo = prog.CurrentElo;
                                }
                                catch (Exception ex)
                                {
                                    Trace.TraceError("Unable to complete debriefing for user " + user.AccountID + ": " + ex);
                                }
                            });
                            debriefings[battleId].partialDebriefing.RatingCategory = category.ToString();
                            debriefings[battleId].debriefingConsumer.Invoke(debriefings[battleId].partialDebriefing);
                            RatingsUpdated(this, new RatingUpdate() { affectedPlayers = playersWithRatingChange });
                        }
                    }
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
                Trace.TraceError("WHR " + category + ": Failed to process battles for rankings: " + ex);
            }
            PendingDebriefing discard;
            debriefings.ForEach(x => pendingDebriefings.TryRemove(x.Key, out discard));
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

        private Game SetupGame(ICollection<int> winners, ICollection<ICollection<int>> losers, int time_step, int id, bool temporary)
        {

            // Avoid self-played games (no info)
            if (winners.Equals(losers))
            {
                Trace.TraceError("White == Black");
                return null;
            }
            if (losers.Count < 1)
            {
                Trace.TraceError("White empty");
                return null;
            }
            if (winners.Count < 1)
            {
                Trace.TraceError("Black empty");
                return null;
            }


            List<ICollection<Player>> white_player = losers.Select(t => (ICollection<Player>)t.Select(p => getPlayerById(p, temporary)).ToList()).ToList();
            List<Player> black_player = winners.Select(p => getPlayerById(p, temporary)).ToList();
            Game game = new Game(black_player, white_player, time_step, id);
            return game;
        }

        private Game CreateGame(ICollection<int> winners, ICollection<ICollection<int>> losers, int time_step, int id)
        {
            Game game = SetupGame(winners, losers, time_step, id, false);
            return game != null ? AddGame(game) : null;
        }

        private Game AddGame(Game game)
        {
            game.loserPlayers.ForEach(t => t.ForEach(p => p.AddGame(game)));
            game.winnerPlayers.ForEach(p => p.AddGame(game));

            return game;
        }

        private void runIterations(int count)
        {
            for (int i = 0; i < count - 1; i++)
            {
                Trace.TraceInformation("Running WHR iteration " + i);
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
