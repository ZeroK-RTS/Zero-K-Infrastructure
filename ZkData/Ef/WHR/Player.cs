
using System;
using System.Collections.Generic;
using System.Diagnostics;
using PlasmaShared;
using ZkData;

namespace Ratings
{
    public class Player
    {

        public readonly int id;
        private readonly RatingCategory category;

        public readonly List<PlayerDay> days = new List<PlayerDay>();
        const float MAX_RATING_CHANGE = 5;

        public float avgElo = 0f / 0;
        public float avgEloVar = 0f / 0;
        public bool onLadder = false;

        private static List<float> hessian_subdiagonal = new List<float>();
        private static List<float> hessian_diagonal = new List<float>();
        
        private static List<float>[] helperLists = new List<float>[4];
        
        private static List<float> covariance_diagonal = new List<float>();
        private static List<float> covariance_subdiagonal = new List<float>();

        private static List<float> hessianLU_lowerSubdiagonal = new List<float>();
        private static List<float> hessianLU_upperDiagonal = new List<float>();
        private static List<float> hessianLU_upperSuperdiagonal = new List<float>();

        private static List<float> gradient = new List<float>();

        private static readonly object whrLock = new object();

        private List<float> sigma2 = new List<float>();
        
        static Player()
        {
            for (int i = 0; i < helperLists.Length; i++)
            {
                helperLists[i] = new List<float>();
            }
        }

        public Player(int id)
        {
            this.id = id;
            this.category = RatingCategory.Casual; // Maps are casual apparently. CBA to make the two different category enums used for rating system (battle type and map type) make sense.
        }

        public Player(int id, RatingCategory category)
        {
            this.id = id;
            this.category = category;
        }

        private void UpdateHessian()
        {

            while (hessian_diagonal.Count < days.Count)
            {
                hessian_diagonal.Add(0f);
                hessian_subdiagonal.Add(0f);
            }
            if (days.Count <= 1)
            {
                hessian_diagonal[0] = days[0].GetLogLikelyhoodSecondDerivative() - 0.001f;
                return;
            }
            for (int i = 1; i < days.Count - 1; i++)
            {
                hessian_diagonal[i] = days[i].GetLogLikelyhoodSecondDerivative() - 1.0f / sigma2[i - 1] - 1.0f / sigma2[i] - 0.001f;
            }
            hessian_diagonal[0] = days[0].GetLogLikelyhoodSecondDerivative() - 1.0f / sigma2[0] - 0.001f;
            hessian_diagonal[days.Count - 1] = days[days.Count - 1].GetLogLikelyhoodSecondDerivative() - 1.0f / sigma2[days.Count - 2] - 0.001f;
            for (int i = 0; i < days.Count - 1; i++)
            {
                hessian_subdiagonal[i] = 1.0f / sigma2[i];
            }
        }


        private void UpdateGradient()
        {
            while (gradient.Count < days.Count)
            {
                gradient.Add(0f);
            }
            if (days.Count <= 1)
            {
                gradient[0] = days[0].GetLogLikelyhoodFirstDerivative();
                return;
            }
            for (int i = 1; i < days.Count - 1; i++)
            {
                gradient[i] = days[i].GetLogLikelyhoodFirstDerivative() - (days[i].r - days[i - 1].r) / sigma2[i - 1] - (days[i].r - days[i + 1].r) / sigma2[i];
            }
            gradient[0] = days[0].GetLogLikelyhoodFirstDerivative() - (days[0].r - days[1].r) / sigma2[0];
            gradient[days.Count - 1] = days[days.Count - 1].GetLogLikelyhoodFirstDerivative() - (days[days.Count - 1].r - days[days.Count - 2].r) / sigma2[days.Count - 2];
        }

        public void RunOneNewtonIteration(bool updateCovariance)
        {
            lock (whrLock)
            {
                foreach (PlayerDay day in days)
                {
                    day.UpdateGameTermsCache();
                }

                if (days.Count == 1)
                {
                    days[0].UpdateByOneDimensionalNewton();
                }
                else if (days.Count > 1)
                {
                    UpdateByNDimNewton();
                }
                if (updateCovariance) UpdateRatingVariance();
            }
        }


        private void UpdateSigma2()
        {

            if (sigma2.Count > 0)
            {
                sigma2[sigma2.Count - 1] = Math.Abs(days[sigma2.Count].day - days[sigma2.Count - 1].day) * GlobalConst.NaturalRatingVariancePerDay(days[sigma2.Count - 1].totalWeight) + GlobalConst.NaturalRatingVariancePerGame * (days[sigma2.Count].totalWeight - days[sigma2.Count - 1].totalWeight);
            }
            for (int i = sigma2.Count; i < days.Count - 1; i++)
            {
                sigma2.Add(Math.Abs(days[i + 1].day - days[i].day) * GlobalConst.NaturalRatingVariancePerDay(days[i].totalWeight) + GlobalConst.NaturalRatingVariancePerGame * (days[i + 1].totalWeight - days[i].totalWeight));
            }
        }


        private void UpdateByNDimNewton()
        {
            // sigma squared (used in the prior)
            UpdateSigma2();

            UpdateHessian();
            UpdateGradient();

            int n = days.Count;

            UpdateHessianLU();

            List<float> y = helperLists[0];
            y[0] = gradient[0];
            for (int i = 1; i < n; i++)
            {
                y[i] = gradient[i] - hessianLU_lowerSubdiagonal[i] * y[i - 1];
            }

            List<float> x = helperLists[1];
            x[n - 1] = y[n - 1] / hessianLU_upperDiagonal[n - 1];
            for (int i = n - 2; i >= 0; i--)
            {
                x[i] = (y[i] - hessianLU_upperSuperdiagonal[i] * x[i + 1]) / hessianLU_upperDiagonal[i];
            }
            
            for (int i = 0; i < days.Count; i++)
            {
                if (float.IsNaN(x[i]))
                {
                    Trace.TraceError("WHR: NaN rating change for player " + id);
                }
                else
                {
                    days[i].r -= Math.Max(-MAX_RATING_CHANGE, Math.Min(MAX_RATING_CHANGE, x[i]));
                }
            }
        }

        private void UpdateHessianLU()
        {

            while (hessianLU_lowerSubdiagonal.Count < days.Count)
            {
                hessianLU_lowerSubdiagonal.Add(0f);
                hessianLU_upperSuperdiagonal.Add(0f);
                hessianLU_upperDiagonal.Add(0f);
                for (int i = 0; i < helperLists.Length; i++)
                {
                    helperLists[i].Add(0f);
                }
            }
            hessianLU_upperDiagonal[0] = hessian_diagonal[0];
            hessianLU_upperSuperdiagonal[0] = hessian_subdiagonal[0];

            for (int i = 1; i < days.Count; i++)
            {
                hessianLU_lowerSubdiagonal[i] = hessian_subdiagonal[i - 1] / hessianLU_upperDiagonal[i - 1];
                hessianLU_upperDiagonal[i] = hessian_diagonal[i] - hessianLU_lowerSubdiagonal[i] * hessianLU_upperSuperdiagonal[i - 1];
                hessianLU_upperSuperdiagonal[i] = hessian_subdiagonal[i];
            }
        }


        private void UpdateCovariance()
        {

            int n = days.Count;

            //a, b, d are taken directly from the update step
            if (hessianLU_lowerSubdiagonal.Count != n || n == 1)
            {
                UpdateSigma2();
                UpdateHessian();
                UpdateHessianLU();
            }

            List<float> dp = helperLists[0];
            dp[n - 1] = hessian_diagonal[n - 1];
            List<float> bp = helperLists[1];
            bp[n - 1] = n >= 2 ? hessian_subdiagonal[n - 2] : 0;
            List<float> ap = helperLists[2];
            for (int i = n - 2; i >= 0; i--)
            {
                ap[i] = hessian_diagonal[i] / dp[i + 1];
                dp[i] = hessian_diagonal[i] - ap[i] * bp[i + 1];
                bp[i] = i > 0 ? hessian_subdiagonal[i - 1] : 0;
            }

            List<float> v = helperLists[3];
            for (int i = 0; i < n - 1; i++)
            {
                v[i] = dp[i + 1] / (hessianLU_upperSuperdiagonal[i] * bp[i + 1] - hessianLU_upperDiagonal[i] * dp[i + 1]);
            }
            v[n - 1] = -1 / hessianLU_upperDiagonal[n - 1];

            while (covariance_diagonal.Count < n)
            {
                covariance_diagonal.Add(0f);
                covariance_subdiagonal.Add(0f);
            }
            for (int i = 0; i < n; i++)
            {
                covariance_diagonal[i] = (v[i]);
                if (i < n - 1)
                {
                    covariance_subdiagonal[i] = (-1 * hessianLU_lowerSubdiagonal[i] * v[i]);
                }
            }
        }

        private void UpdateLadderRatings()
        {

            float avgElo = 0;
            float weightSum = 0;
            float varSum = 0;
            int minAveDay = RatingSystems.ConvertDateToDays(DateTime.UtcNow) - GlobalConst.LadderAverageDays;
            int minActiveDay = RatingSystems.ConvertDateToDays(DateTime.UtcNow) - GlobalConst.LadderActivityDays;
            if (this.category != RatingCategory.MatchMaking)
            {
                minAveDay = minActiveDay;
            }
            for (int i = 0; i < days.Count; i++)
            {
                if (days[i].weight > 0 && days[i].day >= minAveDay) // if any game played that day
                {
                    this.onLadder = true;
                }
                if (days[i].day >= minAveDay)
                {
                    weightSum += days[i].weight;
                    varSum += days[i].weight * days[i].GetEloStdev() * days[i].GetEloStdev();
                    avgElo += days[i].GetElo() * days[i].weight;
                }
            }
            avgElo /= weightSum;
            varSum /= weightSum;
            this.avgEloVar = varSum;
            this.avgElo = avgElo;
        }

        private void UpdateRatingVariance()
        {
            if (days.Count > 0)
            {
                UpdateCovariance();
                for (int i = 0; i < days.Count; i++)
                {
                    days[i].naturalRatingVariance = covariance_diagonal[i];
                }
                UpdateLadderRatings();
            }
        }

        public void AddGame(Game game)
        {
            if (days.Count == 0 || days[days.Count - 1].day != game.day)
            {
                PlayerDay newPDay = new PlayerDay(this, game.day);
                if (days.Count == 0)
                {
                    newPDay.isFirstDay = true;
                    newPDay.totalGames = 2;
                    newPDay.SetGamma(1);
                    newPDay.naturalRatingVariance = 10;
                }
                else
                {
                    newPDay.totalWeight = days[days.Count - 1].totalWeight;
                    newPDay.totalGames = days[days.Count - 1].totalGames;
                    newPDay.SetGamma(days[days.Count - 1].GetGamma());
                    newPDay.naturalRatingVariance = days[days.Count - 1].naturalRatingVariance + (float)Math.Sqrt(game.day - days[days.Count - 1].day) * GlobalConst.NaturalRatingVariancePerDay(days[days.Count - 1].totalWeight);
                }
                days.Add(newPDay);
            }
            if (game.playerFinder.ContainsKey(this))
            {
                game.loserDays[this] = days[days.Count - 1];
            }
            else
            {
                game.winnerDays[this] = days[days.Count - 1];
            }

            days[days.Count - 1].AddGame(game);
        }

        private int FindDayBefore(int date)
        {
            int min = 0;
            int max = days.Count;
            int mid;
            while (max - min > 1)
            {
                mid = (max + min) >> 1;
                if (days[mid].day <= date)
                {
                    min = mid;
                }
                else
                {
                    max = mid;
                }
            }
            return min;
        }

        public void FakeGame(Game game)
        {
            PlayerDay d;
            int insertAfterDay = FindDayBefore(game.day);
            if (days.Count == 0 || days[insertAfterDay].day != game.day)
            {
                PlayerDay new_pday = new PlayerDay(this, game.day);
                if (days.Count == 0)
                {
                    new_pday.isFirstDay = true;
                    new_pday.SetGamma(1);
                }
                else
                {
                    new_pday.SetGamma(days[insertAfterDay].GetGamma());
                }
                d = (new_pday);
            }
            else
            {
                d = days[insertAfterDay];
            }
            if (game.playerFinder.ContainsKey(this))
            {
                game.loserDays[this] = d;
            }
            else
            {
                game.winnerDays[this] = d;
            }
        }

        public void RemoveGame(Game game)
        {
            int dayIndex = FindDayBefore(game.day);
            if (days[dayIndex].day != game.day || !(days[dayIndex].games[0].Contains(game) || days[dayIndex].games[1].Contains(game)))
            {
                Trace.TraceError("Couldn't find game to remove for player " + id);
                return;
            }
            days[dayIndex].games.ForEach(x => x.Remove(game));
            if (!days[dayIndex].isFirstDay && days[dayIndex].games[0].Count + days[dayIndex].games[1].Count == 0) days.RemoveAt(dayIndex);
        }
    }
}