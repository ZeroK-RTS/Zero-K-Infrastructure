
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Ratings
{
    public class Player {

        public int id;
        public List<PlayerDay> days = new List<PlayerDay>();
        public static float w2;
        const float MAX_RATING_CHANGE = 5;

        public Player(int id, float w2) {
            this.id = id;
            Player.w2 = (float)(Math.Pow(Math.Sqrt(w2) * Math.Log(10) / 400, 2));  // Convert from elo^2 to r^2

        }

        float[,] __m = new float[10,10];

        public float[,] generateHessian(List<PlayerDay> days, List<float> sigma2) {

            int n = days.Count;
            if (__m.GetLength(0) < n + 1) {
                __m = new float[n + 10, n + 10];
            }
            float[,] m = __m;
            for (int row = 0; row < n; row++) {
                for (int col = 0; col < n; col++) {
                    if (row == col) {
                        float prior = 0;
                        if (row < (n - 1)) {
                            prior += -1.0f / sigma2[row];
                        }
                        if (row > 0) {
                            prior += -1.0f / sigma2[row - 1];
                        }
                        m[row,col] = days[row].getLogLikelyhoodSecondDerivative() + prior - 0.001f;
                    } else if (row == col - 1) {
                        m[row,col] = 1.0f / sigma2[row];
                    } else if (row == col + 1) {
                        m[row,col] = 1.0f / sigma2[col];
                    } else {
                        m[row,col] = 0;
                    }
                }
            }
            return m;
        }

        public List<float> generateGradient(List<float> r, List<PlayerDay> days, List<float> sigma2) {
            List<float> g = new List<float>();
            int n = days.Count;
            for (int i = 0; i < days.Count; i++) {
                float prior = 0;
                if (i < (n - 1)) {
                    prior += -(r[i] - r[i + 1]) / sigma2[i];
                }
                if (i > 0) {
                    prior += -(r[i] - r[i - 1]) / sigma2[i - 1];
                }
                g.Add(days[i].getLogLikelyhoodFirstDerivative() + prior);
            }
            return g;
        }

        public void runOneNewtonIteration() {
            foreach (PlayerDay day in days) {
                day.clearGameTermsCache();
            }

            if (days.Count == 1) {
                days[0].updateBy1DNewton();
            } else if (days.Count > 1) {
                updateByNDimNewton();
            }
        }

        public List<float> generateSigma2() {
            List<float> sigma2 = new List<float>();
            for (int i = 0; i < days.Count - 1; i++) {
                sigma2.Add(Math.Abs(days[i + 1].day - days[i].day) * w2);
            }
            return sigma2;
        }

        private List<float> makeList(int size) {
            List<float> temp = new List<float>();
            for (int i = 0; i < size; i++) {
                temp.Add(0f);
            }
            return temp;
        }

        public void updateByNDimNewton() {
            List<float> r = new List<float>();
            foreach (PlayerDay day in days) {
                r.Add(day.r);
            }

            if (false) {/*
                Trace.TraceInformation("namein " + id);
                for (PlayerDay day in days) {
                    Trace.TraceInformation("day[#" + day.day + "] r = " + day.r);
                    Trace.TraceInformation("day[#" + day.day + "] win terms = #" + Arrays.deepTostring(day.getWonGameTerms().toArray()) + "");
                    Trace.TraceInformation("day[#" + day.day + "] win games = #" + Arrays.tostring(day.wonGames.toArray()) + "");
                    Trace.TraceInformation("day[#" + day.day + "] lose terms = #" + Arrays.deepTostring(day.getLostGameTerms().toArray()) + "");
                    Trace.TraceInformation("day[#" + day.day + "] lost games = #" + Arrays.tostring(day.lostGames.toArray()) + "");
                    Trace.TraceInformation("day[#" + day.day + "] Log(p) = #" + (day.getLogLikelyhood()) + "");
                    Trace.TraceInformation("day[#" + day.day + "] dlp = #" + (day.getLogLikelyhoodFirstDerivative()) + "");
                    Trace.TraceInformation("day[#" + day.day + "] dlp2 = #" + (day.getLogLikelyhoodSecondDerivative()) + "");
                }*/
            }
            // sigma squared (used in the prior)
            List<float> sigma2 = generateSigma2();

            float[,] h = generateHessian(days, sigma2);
            List<float> g = generateGradient(r, days, sigma2);

            int n = r.Count;

            List<float> a = makeList(n);
            List<float> d = makeList(n);
            List<float> b = makeList(n);
            d[0] = h[0,0];
            b[0] = h[0,1];

            for (int i = 1; i < n; i++) {
                a[i] = ( h[i,i - 1] / d[i - 1]);
                d[i] = ( h[i,i] - a[i] * b[i - 1]);
                b[i] = ( h[i,i + 1]);
            }

            List<float> y = makeList(n);
            y[0] = g[0];
            for (int i = 1; i < n; i++) {
                y[i] = ( g[i] - a[i] * y[i - 1]);
            }

            List<float> x = makeList(n);
            x[n - 1] = y[n - 1] / d[n - 1];
            for (int i = n - 2; i >= 0; i--) {
                x[i] = ( (y[i] - b[i] * x[i + 1]) / d[i]);
            }


            for (int i = 0; i < n; i++) {
                if (Math.Abs(x[i]) > _maxChg) {
                    _maxChg = Math.Abs(x[i]);
                    Trace.TraceWarning("WHR diverging: New max change of " + _maxChg + " for player " + id);
                }
            }

            for (int i = 0; i < days.Count; i++) {
                days[i].r -= Math.Max(-MAX_RATING_CHANGE, Math.Min(MAX_RATING_CHANGE, x[i]));
            }

        }
        static float _maxChg = 3;

        float[,] __cov = new float[10,10];

        public float[,] generateCovariance() {
            List<float> r = new List<float>();
            foreach (PlayerDay day in days) {
                r.Add(day.r);
            }

            List<float> sigma2 = generateSigma2();
            float[,] h = generateHessian(days, sigma2);
            List<float> g = generateGradient(r, days, sigma2);

            int n = r.Count;

            List<float> a = makeList(n);
            List<float> d = makeList(n);
            List<float> b = makeList(n);
            d[0] = h[0, 0];
            b[0] = h[0, 1];

            for (int i = 1; i < n; i++) {
                a[i] = ( h[i,i - 1] / d[i - 1]);
                d[i] = ( h[i,i] - a[i] * b[i - 1]);
                b[i] = ( h[i,i + 1]);
            }

            List<float> dp = makeList(n);
            dp[n - 1] = (h[n - 1,n - 1]);
            List<float> bp = makeList(n);
            bp[n - 1] = (n >= 2 ? h[n - 1,n - 2] : 0);
            List<float> ap = makeList(n);
            for (int i = n - 2; i >= 0; i--) {
                ap[i] = ( h[i,i + 1] / dp[i + 1]);
                dp[i] = ( h[i,i] - ap[i] * bp[i + 1]);
                bp[i] = ( i > 0 ? h[i,i - 1] : 0);
            }

            List<float> v = makeList(n);
            for (int i = 0; i < n - 1; i++) {
                v[i] = ( dp[i + 1] / (b[i] * bp[i + 1] - d[i] * dp[i + 1]));
            }
            v[n - 1] = (-1 / d[n - 1]);

            if (__cov.GetLength(0) < n + 1) {
                __cov = new float[n + 10,n + 10];
            }
            float[,] cov = __cov;

            for (int row = 0; row < n; row++) {
                for (int col = 0; col < n; col++) {
                    if (row == col) {
                        cov[row,col] = v[row];
                    } else if (row == col - 1) {
                        cov[row,col] = -1 * a[col] * v[col];
                    } else {
                        cov[row,col] = 0;
                    }
                }
            }
            return cov;
        }

        public void updateUncertainty() {
            if (days.Count > 0) {
                float[,] c = generateCovariance();
                for (int i = 0; i < days.Count; i++) {
                    days[i].uncertainty = c[i,i];
                }
            }
        }

        public void AddGame(Game game) {
            if (days.Count == 0 || days[days.Count - 1].day != game.day) {
                PlayerDay newPDay = new PlayerDay(this, game.day);
                if (days.Count == 0) {
                    newPDay.isFirstDay = true;
                    newPDay.setGamma(1);
                    newPDay.uncertainty = 10;
                } else {
                    newPDay.setGamma(days[days.Count - 1].getGamma());
                    newPDay.uncertainty = days[days.Count - 1].uncertainty + (float)Math.Sqrt(game.day - days[days.Count - 1].day) * w2;
                }
                days.Add(newPDay);
            }
            if (game.whitePlayers.Contains(this)) {
                game.whiteDays.Add(this, days[days.Count - 1]);
            } else {
                game.blackDays.Add(this, days[days.Count - 1]);
            }

            days[days.Count - 1].AddGame(game);
        }


        public void fakeGame(Game game) {
            PlayerDay d;
            if (days.Count == 0 || days[days.Count - 1].day != game.day)
            {
                PlayerDay newPDay = new PlayerDay(this, game.day);
                if (days.Count == 0)
                {
                    newPDay.isFirstDay = true;
                    newPDay.setGamma(1);
                    newPDay.uncertainty = 10;
                }
                else
                {
                    newPDay.setGamma(days[days.Count - 1].getGamma());
                    newPDay.uncertainty = days[days.Count - 1].uncertainty + (float)Math.Sqrt(game.day - days[days.Count - 1].day) * w2;
                }
                d = (newPDay);
            } else {
                d = days[days.Count - 1];
            }
            if (game.whitePlayers.Contains(this)) {
                game.whiteDays.Add(this, d);
            } else {
                game.blackDays.Add(this, d);
            }
        }

    }

}