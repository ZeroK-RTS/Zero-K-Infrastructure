
using System;
using System.Collections.Generic;
using System.Diagnostics;
using PlasmaShared;
using ZkData;

namespace Ratings
{

    public class Game
    {
        public readonly int day;
        public readonly int id;
        public List<ICollection<Player>> loserPlayers;
        public ICollection<Player> winnerPlayers;
        public Dictionary<Player, PlayerDay> loserDays = new Dictionary<Player, PlayerDay>();
        public Dictionary<Player, PlayerDay> winnerDays = new Dictionary<Player, PlayerDay>();
        public Dictionary<Player, int> playerFinder = new Dictionary<Player, int>();

        public Game(ICollection<Player> winner, List<ICollection<Player>> loser, int time_step, int id)
        {
            this.id = id;
            day = time_step;
            loserPlayers = loser;
            winnerPlayers = winner;
            for (int i = 0; i < loser.Count; i++)
            {
                loser[i].ForEach(p => playerFinder.Add(p, i));
            }
        }

        private float GetLoserNaturalRating(int team)
        {
            float ret = 0;
            float w;
            foreach (Player p in loserPlayers[team])
            {
                PlayerDay pd = loserDays[p];
                w = GetPlayerWeight(pd.player);
                ret += pd.GetNaturalRating() * w;
            }
            return ret;
        }

        private float GetBlackNaturalRating()
        {
            float ret = 0;
            float w;
            foreach (PlayerDay pd in winnerDays.Values)
            {
                w = GetPlayerWeight(pd.player);
                ret += pd.GetNaturalRating() * w;
            }
            return ret;
        }

        public float GetPlayerWeight(Player p)
        {
            int size = playerFinder.ContainsKey(p) ? loserPlayers[playerFinder[p]].Count : winnerPlayers.Count;
            return 1.0f / size;
        }

        private float GetLoserGamma(int team)
        {
            return (float)Math.Exp(GetLoserNaturalRating(team));
        }
        private float GetLosersGamma()
        {
            float sum = 0;
            for (int i = 0; i < loserPlayers.Count; i++) sum += GetLoserGamma(i);
            return sum;
        }

        private float GetWinnerGamma()
        {
            return (float)Math.Exp(GetBlackNaturalRating());
        }

        public float GetOpponentsAdjustedGamma(Player player)
        {
            
            float rval = GetWinnerGamma() + GetLosersGamma() - GetAlliesAdjustedGamma(player, false);
            if (rval == 0 || float.IsInfinity(rval) || float.IsNaN(rval))
            {
                Trace.TraceError("WHR: Gamma out of bounds");
            }
            return rval;
        }

        public float GetAlliesAdjustedGamma(Player player, bool excludeMe = true)
        {

            float ally_naturalrating = 0;
            float blackNaturalRating = GetBlackNaturalRating();
            if ((playerFinder.ContainsKey(player)))
            {
                float whiteNaturalRating = GetLoserNaturalRating(playerFinder[player]);
                ally_naturalrating = whiteNaturalRating ;
                if (excludeMe) ally_naturalrating -= loserDays[player].GetNaturalRating() * GetPlayerWeight(player);
            }
            else
            {
                ally_naturalrating = blackNaturalRating;
                if (excludeMe) ally_naturalrating -= winnerDays[player].GetNaturalRating() * GetPlayerWeight(player);
            }
            float rval = (float)Math.Exp(ally_naturalrating);
            if (rval == 0 || float.IsInfinity(rval) || float.IsNaN(rval))
            {
                Trace.TraceError("WHR: Gamma out of bounds");
            }
            return rval;
        }

        public float GetMyAdjustedGamma(Player player)
        {

            float my_naturalrating = 0;
            if ((playerFinder.ContainsKey(player)))
            {
                my_naturalrating = loserDays[player].GetNaturalRating() * GetPlayerWeight(player);
            }
            else
            {
                my_naturalrating = winnerDays[player].GetNaturalRating() * GetPlayerWeight(player);
            }
            float rval = (float)Math.Exp(my_naturalrating);
            if (rval == 0 || float.IsInfinity(rval) || float.IsNaN(rval))
            {
                Trace.TraceError("WHR: Gamma out of bounds");
            }
            return rval;
        }

        public float GetWinProbability()
        {
            if (loserDays.Count == 0 || winnerDays.Count == 0)
            {
                loserPlayers.ForEach(t => t.ForEach(p => p.FakeGame(this)));
                winnerPlayers.ForEach(p => p.FakeGame(this));
            }
            return GetWinnerGamma() / (GetLosersGamma() + GetWinnerGamma());
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