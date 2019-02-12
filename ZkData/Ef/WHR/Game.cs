
using System;
using System.Collections.Generic;
using System.Diagnostics;
using ZkData;

namespace Ratings
{

    public class Game
    {
        public readonly int day;
        public readonly int id;
        public ICollection<Player> whitePlayers;
        public ICollection<Player> blackPlayers;
        public bool blackWins;
        public Dictionary<Player, PlayerDay> whiteDays = new Dictionary<Player, PlayerDay>();
        public Dictionary<Player, PlayerDay> blackDays = new Dictionary<Player, PlayerDay>();

        public Game(ICollection<Player> black, ICollection<Player> white, bool blackWins, int time_step, int id)
        {
            this.id = id;
            day = time_step;
            whitePlayers = white;
            blackPlayers = black;
            this.blackWins = blackWins;
        }

        private float GetWhiteNaturalRating()
        {
            float ret = 0;
            float w;
            float totWeight = 0;
            foreach (PlayerDay pd in whiteDays.Values)
            {
                w = GetPlayerWeight(pd.player);
                totWeight += w;
                ret += pd.GetNaturalRating() * w;
            }
            return ret / totWeight;
        }

        private float GetBlackNaturalRating()
        {
            float ret = 0;
            float w;
            float totWeight = 0;
            foreach (PlayerDay pd in blackDays.Values)
            {
                w = GetPlayerWeight(pd.player);
                totWeight += w;
                ret += pd.GetNaturalRating() * w;
            }
            return ret / totWeight;
        }

        public float GetPlayerWeight(Player p)
        {
            return 1.0f / GetPlayerTeammates(p).Count;
        }

        private float GetWhiteGamma()
        {
            return (float)Math.Exp(GetWhiteNaturalRating());
        }

        private float GetBlackGamma()
        {
            return (float)Math.Exp(GetBlackNaturalRating());
        }

        public float GetOpponentsAdjustedGamma(Player player)
        {

            float opponent_naturalrating = 0;
            float blackNaturalRating = GetBlackNaturalRating();
            float whiteNaturalRating = GetWhiteNaturalRating();
            if ((whitePlayers.Contains(player)))
            {
                opponent_naturalrating = blackNaturalRating + 0 * (-whiteNaturalRating + whiteDays[player].GetNaturalRating())/* / (float)Math.sqrt(whiteDays.Count)*/;
            }
            else
            {
                opponent_naturalrating = whiteNaturalRating + 0 * (-blackNaturalRating + blackDays[player].GetNaturalRating())/* / (float)Math.sqrt(blackDays.Count)*/;
            }
            float rval = (float)Math.Exp(opponent_naturalrating);
            if (rval == 0 || float.IsInfinity(rval) || float.IsNaN(rval))
            {
                Trace.TraceError("WHR: Gamma out of bounds");
            }
            return rval;
        }

        public float GetAlliesAdjustedGamma(Player player)
        {

            float ally_naturalrating = 0;
            float blackNaturalRating = GetBlackNaturalRating();
            float whiteNaturalRating = GetWhiteNaturalRating();
            if ((whitePlayers.Contains(player)))
            {
                ally_naturalrating = whiteNaturalRating - whiteDays[player].GetNaturalRating() * GetPlayerWeight(player);
            }
            else
            {
                ally_naturalrating = blackNaturalRating - blackDays[player].GetNaturalRating() * GetPlayerWeight(player);
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
            if ((whitePlayers.Contains(player)))
            {
                my_naturalrating = whiteDays[player].GetNaturalRating() * GetPlayerWeight(player);
            }
            else
            {
                my_naturalrating = blackDays[player].GetNaturalRating() * GetPlayerWeight(player);
            }
            float rval = (float)Math.Exp(my_naturalrating);
            if (rval == 0 || float.IsInfinity(rval) || float.IsNaN(rval))
            {
                Trace.TraceError("WHR: Gamma out of bounds");
            }
            return rval;
        }

        public ICollection<Player> GetPlayerTeammates(Player player)
        {
            if ((whitePlayers.Contains(player)))
            {
                return whitePlayers;
            }
            else
            {
                return blackPlayers;
            }
        }

        public float GetBlackWinProbability()
        {
            if (whiteDays.Count == 0 || blackDays.Count == 0)
            {
                whitePlayers.ForEach(p => p.FakeGame(this));
                blackPlayers.ForEach(p => p.FakeGame(this));
            }
            return GetBlackGamma() / (GetWhiteGamma() + GetBlackGamma());
        }
        
        public override int GetHashCode()
        {
            return id;
        }

        public override bool Equals(Object other)
        {
            return other is Game game && game.id == id;
        }
    }
}