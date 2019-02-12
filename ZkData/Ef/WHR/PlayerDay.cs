
using System;
using System.Collections.Generic;
using System.Diagnostics;
using ZkData;

namespace Ratings
{
    public class PlayerDay
    {

        public List<Game>[] games; //wongames, lostgames
        public float totalWeight = 0;
        public int totalGames = 0;
        public readonly int day;
        public readonly Player player;
        public float r;
        public bool isFirstDay;
        public float uncertainty;

        static private List<float> game_terms = new List<float>();
        private const int terms = 4;


        public PlayerDay(Player player, int day)
        {
            this.day = day;
            this.player = player;
            isFirstDay = false;
            this.games = new List<Game>[2];
            this.games[0] = new List<Game>();
            this.games[1] = new List<Game>();
        }

        public float GetEloStdev()
        {
            return (float)Math.Sqrt(uncertainty / GlobalConst.EloToNaturalRatingMultiplierSquared);
        }

        public void SetGamma(float gamma)
        {
            r = (float)Math.Log(gamma);
        }

        public float GetGamma()
        {
            return (float)Math.Exp(r);
        }

        public float GetNaturalRating()
        {
            return r;
        }

        public void SetElo(float elo)
        {
            r = elo * ((float)Math.Log(10) / 400.0f);
        }

        public float GetElo()
        {
            return (r * 400.0f) / ((float)Math.Log(10));
        }


        public void UpdateGameTermsCache()
        {
            UpdateGameTerms();
        }

        private int GetWonStartIndex()
        {
            return (totalGames - games[0].Count - games[1].Count - (isFirstDay ? 2 : 0)) * terms;
        }

        private int GetLostStartIndex()
        {
            return (totalGames - games[1].Count - (isFirstDay ? 1 : 0)) * terms;
        }

        private int GetWonEndIndex()
        {
            return (totalGames - games[1].Count - (isFirstDay ? 1 : 0)) * terms;
        }

        private int GetLostEndIndex()
        {
            return (totalGames) * terms;
        }

        private void UpdateGameTerms()
        {
            while (game_terms.Count < terms * (totalGames))
            {
                game_terms.Add(0f);
            }
            int i = GetWonStartIndex();
            for (int wonLost = 0; wonLost < 2; wonLost++)
            {
                for (int game = 0; game < games[wonLost].Count; game++)
                {
                    game_terms[i++] = games[wonLost][game].GetAlliesAdjustedGamma(player); //allyGamma
                    game_terms[i++] = games[wonLost][game].GetOpponentsAdjustedGamma(player); //other_gamma
                    game_terms[i++] = games[wonLost][game].GetMyAdjustedGamma(player); //myGamma
                    game_terms[i++] = games[wonLost][game].GetPlayerWeight(player); //my weight
                }
                if (isFirstDay)
                {
                    game_terms[i++] = 1f;
                    game_terms[i++] = 1f;
                    game_terms[i++] = GetGamma();
                    game_terms[i++] = 1f;
                }
            }
        }

        private float Square(float f) => f * f;

        public float GetLogLikelyhoodSecondDerivative()
        {
            float sum = 0.0f;
            int lostEnd = GetLostEndIndex();
            for (int i = GetWonStartIndex(); i < lostEnd; i += terms)
            {
                sum -= (game_terms[i + 3] * game_terms[i + 0] * game_terms[i + 3] * game_terms[i + 1] * game_terms[i + 2]) / Square(game_terms[i + 0] * game_terms[i + 2] + game_terms[i + 1]);
            }
            return sum;
        }

        public float GetLogLikelyhoodFirstDerivative()
        {
            float tally = 0.0f;
            int wonEnd = GetWonEndIndex();
            int lostEnd = GetLostEndIndex();
            for (int i = GetWonStartIndex() + 3; i < wonEnd; i += terms)
            {
                tally += game_terms[i];
            }
            for (int i = GetWonStartIndex(); i < lostEnd; i += terms)
            {
                tally -= (game_terms[i + 3] * game_terms[i + 0] * game_terms[i + 2]) / (game_terms[i + 0] * game_terms[i + 2] + game_terms[i + 1]);
            }
            return tally;
        }

        public float GetLogLikelyhood()
        {
            float tally = 0.0f;
            int wonEnd = GetWonEndIndex();
            int lostEnd = GetLostEndIndex();
            for (int i = GetWonStartIndex(); i < wonEnd; i += terms)
            {
                tally += (float)Math.Log(game_terms[i + 0]) + (float)Math.Log(game_terms[i + 2]) - (float)Math.Log(game_terms[i + 0] * game_terms[i + 2] + game_terms[i + 1]);
            }
            for (int i = GetLostStartIndex(); i < lostEnd; i += terms)
            {
                tally += (float)Math.Log(game_terms[i + 1]) - (float)Math.Log(game_terms[i + 0] * game_terms[i + 2] + game_terms[i + 1]);
            }
            return tally;
        }

        public void AddGame(Game game)
        {
            totalWeight += game.GetPlayerWeight(player);
            totalGames++;
            if ((game.blackWins == false && game.whitePlayers.Contains(player))
                    || (game.blackWins == true && game.blackPlayers.Contains(player)))
            {
                games[0].Add(game);
            }
            else if ((game.blackWins == true && game.whitePlayers.Contains(player))
                  || (game.blackWins == false && game.blackPlayers.Contains(player)))
            {
                games[1].Add(game);
            }
        }

        public void UpdateByOneDimensionalNewton()
        {
            float dr = (GetLogLikelyhoodFirstDerivative() / GetLogLikelyhoodSecondDerivative());
            float new_r = r - dr;
            r = new_r;
        }

    }

}