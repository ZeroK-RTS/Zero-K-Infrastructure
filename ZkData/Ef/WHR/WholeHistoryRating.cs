// Implementation of WHR " + category +" based on original by Pete Schwamb httpsin//github.com/goshrine/whole_history_rating

using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        const float RatingOffset = 1500;
        public static readonly PlayerRating DefaultRating = new PlayerRating(int.MaxValue, 1, RatingOffset, float.PositiveInfinity, 0, 0);
        
        IDictionary<ITopPlayersUpdateListener, int> topPlayersUpdateListeners = new Dictionary<ITopPlayersUpdateListener, int>();
        public event EventHandler<RatingUpdate> RatingsUpdated;

        IDictionary<int, PlayerRating> playerOldRatings = new ConcurrentDictionary<int, PlayerRating>();
        IDictionary<int, PlayerRating> playerRatings = new ConcurrentDictionary<int, PlayerRating>();
        IDictionary<int, Player> players = new Dictionary<int, Player>();
        SortedDictionary<float, int> sortedPlayers = new SortedDictionary<float, int>();
        List<int> topPlayers = new List<int>();
        IDictionary<int, float> playerKeys = new Dictionary<int, float>();
        Random rand = new Random();
        readonly float w2; //elo range expand per day squared
        private Timer ladderRecalculationTimer;

        private bool runningInitialization = true;
        private readonly RatingCategory category;

        public WholeHistoryRating(RatingCategory category)
        {
            this.category = category;
            w2 = GlobalConst.EloDecayPerDaySquared;
            ladderRecalculationTimer = new Timer((t) => { UpdateRatings(); }, this, 15 * 60000, (int)(GlobalConst.LadderUpdatePeriod * 3600 * 1000 + 4242));
        }
        

        public PlayerRating GetPlayerRating(int accountID)
        {
            return playerRatings.ContainsKey(accountID) ? playerRatings[accountID] : DefaultRating;
        }

        public Dictionary<DateTime, float> GetPlayerRatingHistory(int accountID)
        {
            if (!players.ContainsKey(accountID)) return new Dictionary<DateTime, float>();
            return players[accountID].days.ToDictionary(day => RatingSystems.ConvertDaysToDate(day.day), day => day.getElo() + RatingOffset);
        }

        public List<float> PredictOutcome(IEnumerable<IEnumerable<Account>> teams)
        {
            return teams.Select(t =>
                    SetupGame(t.Select(x => x.AccountID).ToList(),
                            teams.Where(t2 => !t2.Equals(t)).SelectMany(t2 => t2.Select(x => x.AccountID)).ToList(),
                            true,
                            RatingSystems.ConvertDateToDays(DateTime.UtcNow),
                            -1
                    ).getBlackWinProbability() * 2 / teams.Count()).ToList();
        }

        private int battlesRegistered = 0;
        private SpringBattle firstBattle = null;

        private bool _outoforder = false;

        public void ProcessBattle(SpringBattle battle)
        {
            ICollection<int> winners = battle.SpringBattlePlayers.Where(p => p.IsInVictoryTeam && !p.IsSpectator).Select(p => p.AccountID).ToList();
            ICollection<int> losers = battle.SpringBattlePlayers.Where(p => !p.IsInVictoryTeam && !p.IsSpectator).Select(p => p.AccountID).ToList();
            if (winners.Count > 0 && losers.Count > 0)
            {
                if (latestBattle != null && battle.StartTime < latestBattle.StartTime && !_outoforder)
                {
                    _outoforder = true;
                    Trace.TraceWarning("WHR " + category + " receiving battles out of order! " + battle.SpringBattleID + " from " + battle.StartTime + " comes before " + latestBattle.SpringBattleID + " from " + latestBattle.StartTime);
                }

                if (firstBattle == null) firstBattle = battle;
                latestBattle = battle;
                battlesRegistered++;
                int date = RatingSystems.ConvertDateToDays(battle.StartTime);
                if (date > RatingSystems.ConvertDateToDays(DateTime.UtcNow))
                {   
                    Trace.TraceWarning("WHR " + category +": Tried to register battle " + battle.SpringBattleID + " which is from the future " + (date) + " > " + RatingSystems.ConvertDateToDays(DateTime.UtcNow));
                }
                else
                {
                    createGame(losers, winners, false, date, battle.SpringBattleID);
                    if (RatingSystems.Initialized)
                    {
                        Trace.TraceInformation(battlesRegistered + " battles registered for WHR " + category +", latest Battle: " + battle.SpringBattleID);
                        UpdateRatings();
                        using (var db = new ZkDataContext()) {
                            db.SpringBattlePlayers.Where(p => p.SpringBattleID == battle.SpringBattleID && !p.IsSpectator && playerOldRatings.ContainsKey(p.AccountID)).ForEach(p => p.EloChange = playerRatings[p.AccountID].RealElo - playerOldRatings[p.AccountID].RealElo);
                            db.SaveChanges();
                        }
                        losers.Concat(winners).ForEach(x => playerOldRatings[x] = playerRatings[x]);
                    }
                }
            }
        }

        public List<Account> GetTopPlayers(int count)
        {
            ZkDataContext db = new ZkDataContext();
            List<int> retIDs = topPlayers.Take(count).ToList();
            return db.Accounts.Where(a => retIDs.Contains(a.AccountID)).OrderByDescending(x => x.AccountRatings.Where(r => r.RatingCategory == category).Select(r => r.Elo).DefaultIfEmpty(-1).FirstOrDefault()).ToList(); 
        }

        public List<Account> GetTopPlayers(int count, Func<Account, bool> selector)
        {
            lock (updateLockInternal) 
            {
                int counter = 0;
                ZkDataContext db = new ZkDataContext();
                List<Account> retval = new List<Account>();
                foreach (var pair in sortedPlayers)
                {
                    Account acc = db.Accounts.Where(a => a.AccountID == pair.Value).FirstOrDefault();
                    if (playerRatings[acc.AccountID].Rank < int.MaxValue && selector.Invoke(acc))
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


        private SpringBattle latestBattle, lastUpdate;
        private DateTime lastUpdateTime;

        private readonly object updateLock = new object();
        private readonly object updateLockInternal = new object();
        private readonly object dbLock = new object();

        public void UpdateRatings()
        {
            if (!RatingSystems.Initialized) return;
            if (latestBattle == null)
            {
                //Trace.TraceInformation("WHR " + category +": No battles to evaluate");
                return;
            }
            lock (updateLock)
            {
                Action updateAction = null;
                if (lastUpdate == null)
                {
                    updateAction = (() => {
                        Trace.TraceInformation("Initializing WHR " + category +" ratings for " + battlesRegistered + " battles, this will take some time.. From B" + firstBattle?.SpringBattleID + " to B" + latestBattle?.SpringBattleID);
                        runIterations(50);
                        UpdateRankings(players.Values);
                        SaveToDB();
                    });
                }
                else if (DateTime.UtcNow.Subtract(lastUpdateTime).TotalHours >= GlobalConst.LadderUpdatePeriod)
                {
                    updateAction = (() =>
                    {
                        Trace.TraceInformation("Updating all WHR " + category +" ratings");
                        runIterations(1);
                        UpdateRankings(players.Values);
                        SaveToDB();
                    });
                    lastUpdateTime = DateTime.UtcNow;
                }
                else if (!latestBattle.Equals(lastUpdate))
                {
                    updateAction = (() =>
                    {
                        Trace.TraceInformation("Updating WHR " + category +" ratings for last Battle: " + latestBattle.SpringBattleID);
                        IEnumerable<Player> players = latestBattle.SpringBattlePlayers.Where(p => !p.IsSpectator).Select(p => getPlayerById(p.AccountID));
                        players.ForEach(p => p.runOneNewtonIteration());
                        players.ForEach(p => p.updateUncertainty());
                        UpdateRankings(players);
                        SaveToDB(players.Select(x => x.id));
                    });
                }
                else
                {
                    //Trace.TraceInformation("No WHR " + category +" ratings to update");
                    return;
                }
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        lock (updateLockInternal)
                        {
                            runningInitialization = true;
                            DateTime start = DateTime.Now;
                            updateAction.Invoke();
                            Trace.TraceInformation("WHR " + category +" Ratings updated in " + DateTime.Now.Subtract(start).TotalSeconds + " seconds, " + (GC.GetTotalMemory(false) / (1 << 20)) + "MiB total memory allocated");
                            runningInitialization = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Thread error while updating WHR " + category +" " + ex);
                    }
                });
                lastUpdate = latestBattle;
            }

        }

        public void SaveToDB(IEnumerable<int> players)
        {
            lock (dbLock)
            {

                var db = new ZkDataContext();
                foreach (int player in players)
                {
                    var accountRating = db.AccountRatings.Where(x => x.RatingCategory == category && x.AccountID == player).FirstOrDefault();
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

        public void SaveToDB()
        {
            lock (dbLock)
            {
                DateTime start = DateTime.Now;
                var db = new ZkDataContext();
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
                    if (Math.Abs(playerRatings[accountRating.AccountID].Elo - accountRating.Elo) > 1)
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
                Trace.TraceInformation("WHR " + category +" Ratings saved to DB in " + DateTime.Now.Subtract(start).TotalSeconds + " seconds, " + added + " entries added, " + deleted + " entries removed, " + (GC.GetTotalMemory(false) / (1 << 20)) + "MiB total memory allocated");
            }
        }

        public string DebugPlayer(Account player)
        {
            if (!RatingSystems.Initialized) return "";
            if (!players.ContainsKey(player.AccountID)) return "Unknown player";
            string debugString = "";
            foreach (PlayerDay d in players[player.AccountID].days)
            {
                debugString +=
                    d.day + ";" +
                    d.getElo() + ";" +
                    d.uncertainty * 100 + ";" +
                    d.wonGames.Select(g =>
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
                int currentDay = RatingSystems.ConvertDateToDays(DateTime.UtcNow);
                foreach (var p in players)
                {
                    if (p.days.Count == 0)
                    {
                        Trace.TraceError("WHR " + category +" has invalid player " + p.id + " with no days(games)");
                        continue;
                    }
                    float elo = p.days.Last().getElo() + RatingOffset;
                    float lastUncertainty = p.days.Last().uncertainty * 100;
                    int lastDay = p.days.Last().day;
                    playerRatings[p.id] = new PlayerRating(int.MaxValue, 1, elo, lastUncertainty, lastDay, currentDay);
                    float rating = -playerRatings[p.id].Elo + 0.001f * (float)rand.NextDouble();
                    if (playerKeys.ContainsKey(p.id)) sortedPlayers.Remove(playerKeys[p.id]);
                    playerKeys[p.id] = rating;
                    sortedPlayers[rating] = p.id;
                }
                float[] playerUncertainties = new float[playerRatings.Count];
                int index = 0;
                float DynamicMaxUncertainty = GlobalConst.MinimumDynamicMaxLadderUncertainty;
                foreach (var pair in playerRatings)
                {
                    playerUncertainties[index++] = (float)pair.Value.Uncertainty;
                }
                Array.Sort(playerUncertainties);
                DynamicMaxUncertainty = Math.Max(DynamicMaxUncertainty, playerUncertainties[Math.Min(playerUncertainties.Length, GlobalConst.LadderSize) - 1] + 0.01f);
                int activePlayers = Math.Max(1, ~Array.BinarySearch(playerUncertainties, DynamicMaxUncertainty));
                int rank = 0;
                List<int> newTopPlayers = new List<int>();
                int matched = 0;
                foreach (var pair in sortedPlayers)
                {
                    if (playerRatings[pair.Value].Uncertainty <= DynamicMaxUncertainty)
                    {
                        newTopPlayers.Add(pair.Value);
                        if (rank == matched && rank < topPlayers.Count && topPlayers[rank] == pair.Value) matched++;
                        rank++;
                        playerRatings[pair.Value].ApplyLadderUpdate(rank, (float)rank / activePlayers, currentDay);
                    }
                    else if (playerRatings[pair.Value].Rank < int.MaxValue)
                    { 
                        playerRatings[pair.Value].ApplyLadderUpdate(int.MaxValue, 1, currentDay);
                    }
                }
                topPlayers = newTopPlayers;
                Trace.TraceInformation("WHR " + category +" Ladders updated with " + topPlayers.Count + "/" + this.players.Count + " entries, max uncertainty selected: " + DynamicMaxUncertainty);


                //check for topX updates
                foreach (var listener in topPlayersUpdateListeners)
                {
                    if (matched < listener.Value)
                    {
                        listener.Key.TopPlayersUpdated(GetTopPlayers(listener.Value));
                    }
                }
                RatingsUpdated(this, new RatingUpdate() { affectedPlayers = players.Select(x => x.id) });
            }
            catch (Exception ex)
            {
                string dbg = "WHR " + category +": Failed to update rankings " + ex + "\nPlayers: ";
                foreach (var p in players)
                {
                    dbg += p.id + " (" + p.days.Count + " days), ";
                }
                Trace.TraceError(dbg);
            }
        }

        private Player GetPlayerByAccount(Account acc)
        {
            return getPlayerById(acc.AccountID);
        }

        private Player getPlayerById(int id)
        {
            if (!players.ContainsKey(id))
            {
                lock (updateLockInternal)
                {
                    players.Add(id, new Player(id, w2));
                }
            }
            return players[id];
        }

        private Game SetupGame(ICollection<int> black, ICollection<int> white, bool blackWins, int time_step, int id)
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


            List<Player> white_player = white.Select(p => getPlayerById(p)).ToList();
            List<Player> black_player = black.Select(p => getPlayerById(p)).ToList();
            Game game = new Game(black_player, white_player, blackWins, time_step, id);
            return game;
        }

        private Game createGame(ICollection<int> black, ICollection<int> white, bool blackWins, int time_step, int id)
        {
            Game game = SetupGame(black, white, blackWins, time_step, id);
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
            for (int i = 0; i < count; i++)
            {
                runSingleIteration();
            }
            foreach (Player p in players.Values)
            {
                p.updateUncertainty();
            }
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
                    float elo = p.days[p.days.Count - 1].getElo();
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

        private void runSingleIteration()
        {
            foreach (Player p in players.Values)
            {
                p.runOneNewtonIteration();
            }
        }
    }

    public class RatingUpdate : EventArgs
    {
        public IEnumerable<int> affectedPlayers { get; set; }
    }

}
