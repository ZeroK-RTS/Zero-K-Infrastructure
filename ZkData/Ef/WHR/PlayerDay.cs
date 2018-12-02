
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Ratings
{

    public class PlayerDay
    {

        public ICollection<Game> wonGames, lostGames;
        public int TotalGames = 0;
        public int day;
        public Player player;
        private float _r = 0;
        public float r
        {
            get
            {
                return _r;
            }
            set
            {
                if (float.IsNaN(value)) return;
                float delta = value - _r;
                delta = Math.Max(-Player.MAX_RATING_CHANGE, Math.Min(Player.MAX_RATING_CHANGE, delta));
                _r = Math.Max(-60, Math.Min(60, _r + delta));
            }
        }
        public bool isFirstDay;
        public float uncertainty;

        public PlayerDay(Player player, int day)
        {
            this.day = day;
            this.player = player;
            isFirstDay = false;
            this.wonGames = new List<Game>();
            this.lostGames = new List<Game>();
        }

        public void initGamma(float gamma)
        {
            _r = (float)(Math.Log(gamma));
            r = _r;
        }

        public float getGamma()
        {
            return (float)(Math.Exp(r));
        }

        public void setElo(float elo)
        {
            r = (float)(elo * (Math.Log(10) / 400.0));
        }

        public float getElo()
        {
            return (float)((r * 400.0) / (Math.Log(10)));
        }

        ICollection<float[]> won_game_terms, lost_game_terms;

        public void clearGameTermsCache()
        {
            won_game_terms = null;
            lost_game_terms = null;
        }

        public ICollection<float[]> getWonGameTerms()
        {
            if (won_game_terms == null)
            {
                won_game_terms = new List<float[]>();
                foreach (Game g in wonGames)
                {
                    float other_gamma = g.getOpponentsAdjustedGamma(player);
                    won_game_terms.Add(new float[] { 1.0f, 0.0f, 1.0f, other_gamma, 1f / g.getPlayerTeammates(player).Count/*g.getPlayerWeight(player)*/ });
                }
                if (isFirstDay)
                {
                    won_game_terms.Add(new float[] { 1, 0, 1, 1, 1 });
                }
            }
            return won_game_terms;
        }

        public ICollection<float[]> getLostGameTerms()
        {
            if (lost_game_terms == null)
            {
                lost_game_terms = new List<float[]>();
                foreach (Game g in lostGames)
                {
                    float other_gamma = g.getOpponentsAdjustedGamma(player);
                    lost_game_terms.Add(new float[] { 0.0f, other_gamma, 1.0f, other_gamma, 1f / g.getPlayerTeammates(player).Count/*g.getPlayerWeight(player)*/ });
                }
                if (isFirstDay)
                {
                    lost_game_terms.Add(new float[] { 0, 1, 1, 1, 1 });

                }
            }
            return lost_game_terms;
        }

        public double getLogLikelyhoodSecondDerivative()
        {
            double sum = 0.0f;
            double gamma = getGamma();
            foreach (float[] terms in getWonGameTerms())
            {
                sum += terms[4] * (terms[2] * terms[3]) / (float)Math.Pow(terms[2] * gamma + terms[3], 2);
            }
            foreach (float[] terms in getLostGameTerms())
            {
                sum += terms[4] * (terms[2] * terms[3]) / (float)Math.Pow(terms[2] * gamma + terms[3], 2);
            }
            return -1 * gamma * sum;
        }

        public double getLogLikelyhoodFirstDerivative()
        {
            double tally = 0.0f;
            double gamma = getGamma();
            double size = 0;
            foreach (float[] terms in getWonGameTerms())
            {
                tally += terms[4] * terms[2] / (terms[2] * gamma + terms[3]);
                size += terms[4];
            }
            foreach (float[] terms in getLostGameTerms())
            {
                tally += terms[4] * terms[2] / (terms[2] * gamma + terms[3]);
            }
            return size - gamma * tally;
        }

        public double getLogLikelyhood()
        {
            float tally = 0.0f;
            float gamma = getGamma();
            foreach (float[] terms in getWonGameTerms())
            {
                tally += terms[4] * (float)Math.Log(terms[0] * gamma);
                tally -= terms[4] * (float)Math.Log(terms[2] * gamma + terms[3]);
            }
            foreach (float[] terms in getLostGameTerms())
            {
                tally += terms[4] * (float)Math.Log(terms[1]);
                tally -= terms[4] * (float)(Math.Log(terms[2] * gamma + terms[3]));
            }
            return tally;
        }

        public void AddGame(Game game)
        {
            TotalGames++;
            if ((!game.blackWins && game.whitePlayers.Contains(player))
                    || (game.blackWins && game.blackPlayers.Contains(player)))
            {
                wonGames.Add(game);
            }
            else if ((game.blackWins && game.whitePlayers.Contains(player))
                  || (!game.blackWins && game.blackPlayers.Contains(player)))
            {
                lostGames.Add(game);
            }
            else
            {
                Trace.TraceError("Player not part of game");
            }

        }

        public void updateBy1DNewton()
        {
            float dr = (float)(getLogLikelyhoodFirstDerivative() / getLogLikelyhoodSecondDerivative());
            float new_r = r - dr;
            r = new_r;
        }

    }
}