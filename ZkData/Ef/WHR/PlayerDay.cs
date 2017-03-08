
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
        public double r;
        public bool isFirstDay;
        public double uncertainty;

        public PlayerDay(Player player, int day) {
            this.day = day;
            this.player = player;
            isFirstDay = false;
            this.wonGames = new List<Game>();
            this.lostGames = new List<Game>();
        }

        public void setGamma(double gamma) {
            r = Math.Log(gamma);
        }

        public double getGamma() {
            return Math.Exp(r);
        }

        public void setElo(double elo) {
            r = elo * (Math.Log(10) / 400.0);
        }

        public double getElo() {
            return (r * 400.0) / (Math.Log(10));
        }

        ICollection<double[]> won_game_terms, lost_game_terms;

        public void clearGameTermsCache() {
            won_game_terms = null;
            lost_game_terms = null;
        }

        public ICollection<double[]> getWonGameTerms() {
            if (won_game_terms == null) {
                won_game_terms = new List<double[]>();
                foreach (Game g in wonGames) {
                    double other_gamma = g.getOpponentsAdjustedGamma(player);
                    won_game_terms.Add(new double[] { 1.0, 0.0, 1.0, other_gamma, 1d / g.getPlayerTeammates(player).Count/*g.getPlayerWeight(player)*/ });
                }
                if (isFirstDay) {
                    won_game_terms.Add(new double[] { 1, 0, 1, 1, 1 });
                }
            }
            return won_game_terms;
        }

        public ICollection<double[]> getLostGameTerms() {
            if (lost_game_terms == null) {
                lost_game_terms = new List<double[]>();
                foreach (Game g in lostGames) {
                    double other_gamma = g.getOpponentsAdjustedGamma(player);
                    lost_game_terms.Add(new double[] { 0.0, other_gamma, 1.0, other_gamma, 1d / g.getPlayerTeammates(player).Count/*g.getPlayerWeight(player)*/ });
                }
                if (isFirstDay) {
                    lost_game_terms.Add(new double[] { 0, 1, 1, 1, 1 });

                }
            }
            return lost_game_terms;
        }

        public double getLogLikelyhoodSecondDerivative() {
            double sum = 0.0;
            double gamma = getGamma();
            foreach (double[] terms in getWonGameTerms()) {
                sum += terms[4] * (terms[2] * terms[3]) / Math.Pow(terms[2] * gamma + terms[3], 2);
            }
            foreach (double[] terms in getLostGameTerms()) {
                sum += terms[4] * (terms[2] * terms[3]) / Math.Pow(terms[2] * gamma + terms[3], 2);
            }
            return -1 * gamma * sum;
        }

        public double getLogLikelyhoodFirstDerivative() {
            double tally = 0.0;
            double gamma = getGamma();
            double size = 0;
            foreach (double[] terms in getWonGameTerms()) {
                tally += terms[4] * terms[2] / (terms[2] * gamma + terms[3]);
                size += terms[4];
            }
            foreach (double[] terms in getLostGameTerms()) {
                tally += terms[4] * terms[2] / (terms[2] * gamma + terms[3]);
            }
            return size - gamma * tally;
        }

        public double getLogLikelyhood() {
            double tally = 0.0;
            double gamma = getGamma();
            foreach (double[] terms in getWonGameTerms()) {
                tally += terms[4] * Math.Log(terms[0] * gamma);
                tally -= terms[4] * Math.Log(terms[2] * gamma + terms[3]);
            }
            foreach (double[] terms in getLostGameTerms()) {
                tally += terms[4] * Math.Log(terms[1]);
                tally -= terms[4] * Math.Log(terms[2] * gamma + terms[3]);
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
            double dr = (getLogLikelyhoodFirstDerivative() / getLogLikelyhoodSecondDerivative());
            double new_r = r - dr;
            r = new_r;
        }

    }
}