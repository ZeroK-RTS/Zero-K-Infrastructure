
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Ratings
{

    public class PlayerDay {

        public ICollection<Game> wonGames, lostGames;
        public string name;
        public int day;
        public Player player;
        public float r;
        public bool isFirstDay;
        public float uncertainty;

        public PlayerDay(Player player, int day) {
            this.day = day;
            this.player = player;
            isFirstDay = false;
            this.wonGames = new List<Game>();
            this.lostGames = new List<Game>();
        }

        public void setGamma(float gamma) {
            r = (float)(Math.Log(gamma));
        }

        public float getGamma() {
            return (float)(Math.Exp(r));
        }

        public void setElo(float elo) {
            r = (float)(elo * (Math.Log(10) / 400.0));
        }

        public float getElo() {
            return (float)((r * 400.0) / (Math.Log(10)));
        }

        ICollection<float[]> won_game_terms, lost_game_terms;

        public void clearGameTermsCache() {
            won_game_terms = null;
            lost_game_terms = null;
        }

        public ICollection<float[]> getWonGameTerms() {
            if (won_game_terms == null) {
                won_game_terms = new List<float[]>();
                foreach (Game g in wonGames) {
                    float other_gamma = g.getOpponentsAdjustedGamma(player);
                    won_game_terms.Add(new float[] { 1.0f, 0.0f, 1.0f, other_gamma, 1f / g.getPlayerTeammates(player).Count/*g.getPlayerWeight(player)*/ });
                }
                if (isFirstDay) {
                    won_game_terms.Add(new float[] { 1, 0, 1, 1, 1 });
                }
            }
            return won_game_terms;
        }

        public ICollection<float[]> getLostGameTerms() {
            if (lost_game_terms == null) {
                lost_game_terms = new List<float[]>();
                foreach (Game g in lostGames) {
                    float other_gamma = g.getOpponentsAdjustedGamma(player);
                    lost_game_terms.Add(new float[] { 0.0f, other_gamma, 1.0f, other_gamma, 1f / g.getPlayerTeammates(player).Count/*g.getPlayerWeight(player)*/ });
                }
                if (isFirstDay) {
                    lost_game_terms.Add(new float[] { 0, 1, 1, 1, 1 });

                }
            }
            return lost_game_terms;
        }

        public float getLogLikelyhoodSecondDerivative() {
            float sum = 0.0f;
            float gamma = getGamma();
            foreach (float[] terms in getWonGameTerms()) {
                sum += terms[4] * (terms[2] * terms[3]) / (float)Math.Pow(terms[2] * gamma + terms[3], 2);
            }
            foreach (float[] terms in getLostGameTerms()) {
                sum += terms[4] * (terms[2] * terms[3]) / (float)Math.Pow(terms[2] * gamma + terms[3], 2);
            }
            return -1 * gamma * sum;
        }

        public float getLogLikelyhoodFirstDerivative() {
            float tally = 0.0f;
            float gamma = getGamma();
            float size = 0;
            foreach (float[] terms in getWonGameTerms()) {
                tally += terms[4] * terms[2] / (terms[2] * gamma + terms[3]);
                size += terms[4];
            }
            foreach (float[] terms in getLostGameTerms()) {
                tally += terms[4] * terms[2] / (terms[2] * gamma + terms[3]);
            }
            return size - gamma * tally;
        }

        public float getLogLikelyhood() {
            float tally = 0.0f;
            float gamma = getGamma();
            foreach (float[] terms in getWonGameTerms()) {
                tally += terms[4] * (float)Math.Log(terms[0] * gamma);
                tally -= terms[4] * (float)Math.Log(terms[2] * gamma + terms[3]);
            }
            foreach (float[] terms in getLostGameTerms()) {
                tally += terms[4] * (float)Math.Log(terms[1]);
                tally -= terms[4] * (float)(Math.Log(terms[2] * gamma + terms[3]));
            }
            return tally;
        }

        public void AddGame(Game game) {
            if ((game.winner.ToUpper().Equals("W") && game.whitePlayers.Contains(player))
                    || (game.winner.ToUpper().Equals("B") && game.blackPlayers.Contains(player))) {
                wonGames.Add(game);
            } else if ((game.winner.ToUpper().Equals("B") && game.whitePlayers.Contains(player))
                    || (game.winner.ToUpper().Equals("W") && game.blackPlayers.Contains(player))) {
                lostGames.Add(game);
            } else {
                Trace.TraceError("Player not part of game");
            }

        }

        public void updateBy1DNewton() {
            float dr = (getLogLikelyhoodFirstDerivative() / getLogLikelyhoodSecondDerivative());
            float new_r = r - dr;
            r = new_r;
        }

    }
}