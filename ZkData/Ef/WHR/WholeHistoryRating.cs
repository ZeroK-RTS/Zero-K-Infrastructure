// Implementation of WHR based on original by Pete Schwamb httpsin//github.com/goshrine/whole_history_rating

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ZkData;

namespace Ratings
{

    public class WholeHistoryRating : IRatingSystem{

        const double DecayPerDaySquared = 300;



        IDictionary<int, Player> players;
        List<Game> games;
        double w2; //elo range expand per day squared

        public WholeHistoryRating() {
            this.w2 = DecayPerDaySquared;
            games = new List<Game>();
            players = new Dictionary<int, Player>();

            ZkDataContext data = new ZkDataContext();
            foreach (SpringBattle b in data.SpringBattles)
            {
                if (!(b.IsMission || b.HasBots || (b.PlayerCount < 2) || (b.ResourceByMapResourceID.MapIsSpecial == true)))
                {
                    ProcessBattle(b);
                }
            }
        }
        

        public double GetPlayerRating(Account account)
        {
            List<double[]> ratings = getPlayerRatings(account.AccountID);
            return ratings.Count > 0 ? ratings.Last()[1] : 0;
        }

        public double GetPlayerRatingUncertainty(Account account)
        {
            List<double[]> ratings = getPlayerRatings(account.AccountID);
            return ratings.Count > 0 ? ratings.Last()[2] : Double.PositiveInfinity;
        }

        public List<double> PredictOutcome(List<List<Account>> teams)
        {
            return teams.Select(t => SetupGame(t, teams.Where(t2 => !t2.Equals(t)).SelectMany(t2 => t2).ToList(), "B", ConvertDate(DateTime.Now)).getBlackWinProbability() * 2 / teams.Count).ToList();
        }

        public void ProcessBattle(SpringBattle battle)
        {
            List<Account> winners = battle.SpringBattlePlayers.Where(p => p.IsInVictoryTeam).Select(p => p.Account).ToList();
            List<Account> losers = battle.SpringBattlePlayers.Where(p => !p.IsInVictoryTeam).Select(p => p.Account).ToList();
            createGame(losers, winners, "W", ConvertDate(battle.StartTime));
        }

        //implementation specific

        public void UpdateAllRatings()
        {
            runIterations(1);
        }

        //private

        private int ConvertDate(DateTime date)
        {
            return (int)date.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalDays;
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

        private List<double[]> getPlayerRatings(int id) {
            Player player = getPlayerById(id);
            return player.days.Select(d=> new double[] { d.day, (d.getElo()), ((d.uncertainty * 100)) }).ToList();
        }

        private Game SetupGame(List<Account> black, List<Account> white, string winner, int time_step) {

            // Avoid self-played games (no info)
            if (black.Equals(white)) {
                Debug.WriteLine("White == Black");
                return null;
            }
            if (white.Count < 1)
            {
                Debug.WriteLine("White empty");
                return null;
            }
            if (black.Count < 1)
            {
                Debug.WriteLine("Black empty");
                return null;
            }


            List<Player> white_player = white.Select(p=>GetPlayerByAccount(p)).ToList();
            List<Player> black_player = black.Select(p=>GetPlayerByAccount(p)).ToList();
            Game game = new Game(black_player, white_player, winner, time_step);
            return game;
        }

        private Game createGame(List<Account> black, List<Account> white, string winner, int time_step) {
            Game game = SetupGame(black, white, winner, time_step);
            return game != null ? AddGame(game) : null;
        }

        private Game AddGame(Game game) {
            game.whitePlayers.ForEach(p=>p.AddGame(game));
            game.blackPlayers.ForEach(p=>p.AddGame(game));

            games.Add(game);
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
            double sum = 0;
            int bigger = 0;
            int total = 0;
            double lowest = 0;
            double highest = 0;
            foreach (Player p in players.Values) {
                if (p.days.Count > 0) {
                    total++;
                    double elo = p.days[p.days.Count - 1].getElo();
                    sum += elo;
                    if (elo > 0) bigger++;
                    lowest = Math.Min(lowest, elo);
                    highest = Math.Max(highest, elo);
                }
            }
            Debug.WriteLine("Lowest eloin " + lowest);
            Debug.WriteLine("Highest eloin " + highest);
            Debug.WriteLine("sum eloin " + sum);
            Debug.WriteLine("Average eloin " + (sum / total));
            Debug.WriteLine("Amount > 0in " + bigger);
            Debug.WriteLine("Amount < 0in " + (total - bigger));
        }

        private void runSingleIteration() {
            foreach (Player p in players.Values) {
                p.runOneNewtonIteration();
            }
        }
    }

}