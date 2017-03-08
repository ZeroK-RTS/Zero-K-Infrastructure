
using System;
using System.Collections.Generic;
using System.Diagnostics;
using ZkData;

namespace Ratings
{
    public class Game {

        public int day;
        public ICollection<Player> whitePlayers;
        public ICollection<Player> blackPlayers;
        public string winner;
        public IDictionary<Player, PlayerDay> whiteDays = new Dictionary<Player, PlayerDay>();
        public IDictionary<Player, PlayerDay> blackDays = new Dictionary<Player, PlayerDay>();

        public Game(ICollection<Player> black, ICollection<Player> white, string winner, int time_step) { //extras?

            day = time_step;
            whitePlayers = white;
            blackPlayers = black;
            this.winner = winner;
        }

        private double totWeight;
        private double getWhiteElo() {
            double ret = 0;
            double w; totWeight = 0;
            foreach (PlayerDay pd in whiteDays.Values) {
                w = 1; //Math.Max(0.1, Math.Min(10, 1 / pd.uncertainty));
                       //Trace.TraceInformation(w);
                totWeight += w;
                ret += pd.getElo() * w;
            }
            //Trace.TraceInformation(totWeight + "\n");
            return ret / totWeight;
        }

        private double getBlackElo() {
            double ret = 0;
            double w; totWeight = 0;
            foreach (PlayerDay pd in blackDays.Values) {
                w = 1;//Math.Max(0.1, Math.Min(10, 1 / pd.uncertainty));
                totWeight += w;
                ret += pd.getElo() * w;
            }
            return ret / totWeight;
        }

        public double getPlayerWeight(Player p) {
            if (whiteDays.ContainsKey(p)) {
                getWhiteElo();
                return Math.Max(0.1, Math.Min(10, 1 / whiteDays[p].uncertainty)) / totWeight;
            }
            getBlackElo();
            return Math.Max(0.1, Math.Min(10, 1 / blackDays[p].uncertainty)) / totWeight;
        }


        private double getWhiteGamma() {
            return Math.Exp(getWhiteElo() * Math.Log(10) / 400.0);
        }
        private double getBlackGamma() {
            return Math.Exp(getBlackElo() * Math.Log(10) / 400.0);
        }
        //*/
        /*
        private double getWhiteGamma(){
            double ret = 0;
            for (PlayerDay pd in whiteDays.Values){
                ret += pd.getGamma();
            }
            return ret / whiteDays.Count;
        }

        private double getBlackGamma(){
            double ret = 0;
            for (PlayerDay pd in blackDays.Values){
                ret += pd.getGamma();
            }
            return ret / blackDays.Count;
        }

        private double getWhiteElo(){
            return Math.Log(getWhiteGamma()) * 400 / Math.Log(10);
        }
        private double getBlackElo(){
            return Math.Log(getBlackGamma()) * 400 / Math.Log(10);
        }
    //*/
        public double getOpponentsAdjustedGamma(Player player) {

            double opponentElo;
            double blackElo = getBlackElo();
            double whiteElo = getWhiteElo();
            if ((whitePlayers.Contains(player))) {
                opponentElo = blackElo + (-whiteElo + whiteDays[player].getElo())/* / Math.Sqrt(whiteDays.Count)*/;
            } else if (blackPlayers.Contains(player)) {
                opponentElo = whiteElo + (-blackElo + blackDays[player].getElo())/* / Math.Sqrt(blackDays.Count)*/;
            } else {
                Trace.TraceError("No opponent for player " + player.id + ", since they're not in this game.");
                return 0;
            }
            double rval = Math.Pow(10, (opponentElo / 400.0));
            if (rval == 0 || double.IsInfinity(rval) || double.IsNaN(rval)) {
                Trace.TraceError("WHR Failure: Gamma out of bounds");
                return 0;
            }
            return rval;
        }

        public ICollection<Player> getPlayerTeammates(Player player) {
            if ((whitePlayers.Contains(player))) {
                return whitePlayers;
            } else if (blackPlayers.Contains(player)) {
                return blackPlayers;
            } else {
                Trace.TraceInformation("No opponent for player " + player.id + ", since they're not in this gamein.");
                return null;
            }
        }

        public double getWhiteWinProbability() {
            if (whiteDays.Count == 0 || blackDays.Count == 0 ) {
                whitePlayers.ForEach(p=>p.fakeGame(this));
                blackPlayers.ForEach(p=>p.fakeGame(this));
            }
            return getWhiteGamma() / (getWhiteGamma() + getBlackGamma());
        }

        public double getBlackWinProbability()
        {
            if (whiteDays.Count == 0 || blackDays.Count == 0)
            {
                whitePlayers.ForEach(p => p.fakeGame(this));
                blackPlayers.ForEach(p => p.fakeGame(this));
            }
            return getBlackGamma() / (getBlackGamma() + getWhiteGamma());
        }
    }
}