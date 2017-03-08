// Implementation of WHR based on original by Pete Schwamb httpsin//github.com/goshrine/whole_history_rating

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZkData;

namespace Ratings
{

    public class WholeHistoryRating : IRatingSystem{

        const float DecayPerDaySquared = 300;
        const float RatingOffset = 1500;


        IDictionary<int, Player> players;
        float w2; //elo range expand per day squared

        public WholeHistoryRating() {
            w2 = DecayPerDaySquared;
            players = new Dictionary<int, Player>();
        }
        

        public float GetPlayerRating(Account account)
        {
            if (!RatingSystems.Initialized) return RatingOffset;
            UpdateRatings();
            ICollection<float[]> ratings = getPlayerRatings(account.AccountID);
            return (ratings.Count > 0 ? ratings.Last()[1] : 0) + RatingOffset; //1500 for zk peoplers to feel at home
        }

        public float GetPlayerRatingUncertainty(Account account)
        {
            if (!RatingSystems.Initialized) return float.PositiveInfinity;
            UpdateRatings();
            Player player = getPlayerById(account.AccountID);
            if (player.days.Count > 0) {
                return player.days.Last().uncertainty * 100 + (float)Math.Sqrt((ConvertDate(DateTime.Now) - player.days.Last().day) * w2) ; 
            }
            return float.PositiveInfinity;
        }

        public List<float> PredictOutcome(List<ICollection<Account>> teams)
        {
            return teams.Select(t => 
                    SetupGame(t.Select(x => x.AccountID).ToList(), 
                            teams.Where(t2 => !t2.Equals(t)).SelectMany(t2 => t2.Select(x => x.AccountID)).ToList(), 
                            true, 
                            ConvertDate(DateTime.Now)).getBlackWinProbability() * 2 / teams.Count
                    ).ToList();
        }

        private int battlesRegistered = 0;

        public void ProcessBattle(SpringBattle battle)
        {
            latestBattle = battle;
            ICollection<int> winners = battle.SpringBattlePlayers.Where(p => p.IsInVictoryTeam).Select(p => p.AccountID).ToList();
            ICollection<int> losers = battle.SpringBattlePlayers.Where(p => !p.IsInVictoryTeam).Select(p => p.AccountID).ToList();
            if (winners.Count > 0 && losers.Count > 0)
            {
                createGame(losers, winners, false, ConvertDate(battle.StartTime));
                if (RatingSystems.Initialized)
                {
                    Trace.TraceInformation(battlesRegistered + " battles registered for WHR");
                    UpdateRatings();
                }
            }
        }

        //implementation specific

        private SpringBattle latestBattle, lastUpdate;

        private readonly object updateLock = new object();
        private readonly object updateLockInternal = new object();

        public void UpdateRatings()
        {
            if (latestBattle == null)
            {
                //Trace.TraceInformation("WHR: No battles to evaluate");
                return;
            }
            if (latestBattle.Equals(lastUpdate))
            {
                //Trace.TraceInformation("WHR: Nothing to update");
                return;
            }
            lock (updateLock)
            {
                Action updateAction = null;
                if (lastUpdate == null)
                {
                    updateAction = (() => {
                        Trace.TraceInformation("Initializing WHR ratings for " + battlesRegistered + " battles, this will take some time..");
                        runIterations(50);
                    });
                }
                else if (latestBattle.StartTime.Subtract(lastUpdate.StartTime).TotalDays > 0.5d)
                {
                    updateAction = (() => {
                        Trace.TraceInformation("Updating all WHR ratings");
                        runIterations(1);
                    });
                }
                else if (!latestBattle.Equals(lastUpdate))
                {
                    updateAction = (() => {
                        Trace.TraceInformation("Updating WHR ratings for last Battle");
                        IEnumerable<Player> players = latestBattle.SpringBattlePlayers.Select(p => getPlayerById(p.AccountID));
                        players.ForEach(p => p.runOneNewtonIteration());
                        players.ForEach(p => p.updateUncertainty());
                    });
                }
                else
                {
                    //Trace.TraceInformation("No WHR ratings to update");
                    return;
                }
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        lock (updateLockInternal)
                        {
                            DateTime start = DateTime.Now;
                            updateAction.Invoke();
                            Trace.TraceInformation("WHR Ratings updated in " + DateTime.Now.Subtract(start).TotalSeconds + " seconds");
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Thread error while updating WHR " + ex);
                    }
                });
                lastUpdate = latestBattle;
            }
            
        }

        //private

        private int ConvertDate(DateTime date)
        {
            return (int)(date.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalDays / 1);
        }

        private Player GetPlayerByAccount(Account acc)
        {
            return getPlayerById(acc.AccountID);
        }

        private Player getPlayerById(int id) {
            if (!players.ContainsKey(id)) {
                players.Add(id, new Player(id, w2));
            }
            return players[id];
        }

        private List<float[]> getPlayerRatings(int id) {
            Player player = getPlayerById(id);
            return player.days.Select(d=> new float[] { d.day, (d.getElo()), ((d.uncertainty * 100)) }).ToList();
        }

        private Game SetupGame(ICollection<int> black, ICollection<int> white, bool blackWins, int time_step) {

            // Avoid self-played games (no info)
            if (black.Equals(white)) {
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


            List<Player> white_player = white.Select(p=> getPlayerById(p)).ToList();
            List<Player> black_player = black.Select(p=> getPlayerById(p)).ToList();
            Game game = new Game(black_player, white_player, blackWins, time_step);
            return game;
        }

        private Game createGame(ICollection<int> black, ICollection<int> white, bool blackWins, int time_step) {
            Game game = SetupGame(black, white, blackWins, time_step);
            return game != null ? AddGame(game) : null;
        }

        private Game AddGame(Game game) {
            game.whitePlayers.ForEach(p=>p.AddGame(game));
            game.blackPlayers.ForEach(p=>p.AddGame(game));
            
            return game;
        }

        private void runIterations(int count) {
            for (int i = 0; i < count; i++) {
                runSingleIteration();
            }
            foreach (Player p in players.Values) {
                p.updateUncertainty();
            }
        }

        private void printStats() {
            float sum = 0;
            int bigger = 0;
            int total = 0;
            float lowest = 0;
            float highest = 0;
            foreach (Player p in players.Values) {
                if (p.days.Count > 0) {
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

        private void runSingleIteration() {
            foreach (Player p in players.Values) {
                p.runOneNewtonIteration();
            }
        }
    }

}
