
using System;
using System.Collections.Generic;
using System.Diagnostics;
using ZkData;

namespace Ratings
{
    public class Game {

        public int day;
        public int id;
        public ICollection<Player> whitePlayers;
        public ICollection<Player> blackPlayers;
        public bool blackWins;
        public IDictionary<Player, PlayerDay> whiteDays = new Dictionary<Player, PlayerDay>();
        public IDictionary<Player, PlayerDay> blackDays = new Dictionary<Player, PlayerDay>();

        public Game(ICollection<Player> black, ICollection<Player> white, bool blackWins, int time_step, int id) { //extras?

            day = time_step;
            whitePlayers = white;
            blackPlayers = black;
            this.blackWins = blackWins;
            this.id = id;
        }

        private float totWeight;
        private float getWhiteElo() {
            float ret = 0;
            float w; totWeight = 0;
            foreach (PlayerDay pd in whiteDays.Values) {
                w = 1; //Math.Max(0.1, Math.Min(10, 1 / pd.uncertainty));
                       //Trace.TraceInformation(w);
                totWeight += w;
                ret += pd.getElo() * w;
            }
            if (whiteDays.Count == 0)
            {
                Trace.TraceError(whitePlayers.Count + "players, but no white days for B" + id);
                return 0;
            }
            //Trace.TraceInformation(totWeight + "\n");
            return ret / totWeight;
        }

        private float getBlackElo() {
            float ret = 0;
            float w; totWeight = 0;
            foreach (PlayerDay pd in blackDays.Values) {
                w = 1;//Math.Max(0.1, Math.Min(10, 1 / pd.uncertainty));
                totWeight += w;
                ret += pd.getElo() * w;
            }
            if (blackDays.Count == 0)
            {
                Trace.TraceError(blackPlayers.Count + "players, but no black days for B" + id);
                return 0;
            }
            return ret / totWeight;
        }

        public float getPlayerWeight(Player p) {
            if (whiteDays.ContainsKey(p)) {
                getWhiteElo();
                return Math.Max(0.1f, Math.Min(10, 1 / whiteDays[p].uncertainty)) / totWeight;
            }
            getBlackElo();
            return Math.Max(0.1f, Math.Min(10, 1 / blackDays[p].uncertainty)) / totWeight;
        }


        private float getWhiteGamma() {
            return (float)(Math.Exp(getWhiteElo() * Math.Log(10) / 400.0f));
        }
        private float getBlackGamma() {
            return (float)(Math.Exp(getBlackElo() * Math.Log(10) / 400.0f));
        }
        //*/
        /*
        private float getWhiteGamma(){
            float ret = 0;
            for (PlayerDay pd in whiteDays.Values){
                ret += pd.getGamma();
            }
            return ret / whiteDays.Count;
        }

        private float getBlackGamma(){
            float ret = 0;
            for (PlayerDay pd in blackDays.Values){
                ret += pd.getGamma();
            }
            return ret / blackDays.Count;
        }

        private float getWhiteElo(){
            return Math.Log(getWhiteGamma()) * 400 / Math.Log(10);
        }
        private float getBlackElo(){
            return Math.Log(getBlackGamma()) * 400 / Math.Log(10);
        }
    //*/
        public float getOpponentsAdjustedGamma(Player player) {

            float opponentElo;
            float blackElo = getBlackElo();
            float whiteElo = getWhiteElo();
            if ((whitePlayers.Contains(player))) {
                opponentElo = blackElo + (-whiteElo + whiteDays[player].getElo())/* / Math.Sqrt(whiteDays.Count)*/;
            } else if (blackPlayers.Contains(player)) {
                opponentElo = whiteElo + (-blackElo + blackDays[player].getElo())/* / Math.Sqrt(blackDays.Count)*/;
            } else {
                Trace.TraceError("No opponent for player " + player.id + ", since they're not in this game.");
                return 0;
            }
            float rval = (float)(Math.Pow(10, (opponentElo / 400.0)));
            if (rval == 0 || float.IsInfinity(rval) || float.IsNaN(rval)) {
                Trace.TraceError("WHR Failure: Gamma out of bounds: " + rval);
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

        public float getWhiteWinProbability() {
            if (whiteDays.Count == 0 || blackDays.Count == 0 ) {
                whitePlayers.ForEach(p=>p.fakeGame(this));
                blackPlayers.ForEach(p=>p.fakeGame(this));
            }
            return getWhiteGamma() / (getWhiteGamma() + getBlackGamma());
        }

        public float getBlackWinProbability()
        {
            if (whiteDays.Count == 0 || blackDays.Count == 0)
            {
                whitePlayers.ForEach(p => p.fakeGame(this));
                blackPlayers.ForEach(p => p.fakeGame(this));
            }
            return getBlackGamma() / (getBlackGamma() + getWhiteGamma());
        }

        public override int GetHashCode()
        {
            return id;
        }

        public override bool Equals(Object other)
        {
            Game game = other as Game;
            return game != null && game.id == id;
        }
    }
}