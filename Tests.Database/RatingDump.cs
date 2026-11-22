using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using PlasmaShared;
using Ratings;
using ZkData;

namespace Tests.Database
{
    /// <summary>
    /// Writes what the rating pipeline produced, in the format ZkData.Core's <c>rate</c>
    /// command writes on .NET 9, so the two runs can be diffed.
    ///
    ///     ./db/run-db-tests.sh --dump-ratings /src/ef6-ratings.tsv
    ///
    /// This is not a test. It is the .NET Framework half of one comparison: same fixture,
    /// same pipeline source - ZkData/Ef/WHR/*.cs, linked into ZkData.Core rather than copied
    /// - and a different data layer underneath.
    ///
    /// It drives RatingSystems.Init itself rather than reusing RatingPipeline, which the
    /// tests use: that one calls ForceRatingsUpdate to make sure ratings exist promptly, and
    /// a second forced pass is exactly what makes the iteration count non-deterministic. The
    /// steps below mirror ZkData.Core/RatingRun.cs one for one, on purpose.
    /// </summary>
    public static class RatingDump
    {
        public static int Run(string outputPath)
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ZK_CONNECTION_STRING")))
            {
                Console.Error.WriteLine("ZK_CONNECTION_STRING is not set. See db/README.md.");
                return 2;
            }

            int battles;
            using (var db = new ZkDataContext())
            {
                battles = db.SpringBattles.Count();
                if (battles == 0)
                {
                    Console.Error.WriteLine("no battles in this database - load the fixture first");
                    return 2;
                }
                // The fixture carries the live pipeline's stored ratings; left in place,
                // GetPlayerRating answers from that cache and the computation never runs.
                db.Database.ExecuteSqlCommand("DELETE FROM AccountRatings");
            }

            var clock = Stopwatch.StartNew();
            RatingSystems.Init();

            if (!Wait(() => RatingSystems.Initialized, TimeSpan.FromMinutes(3)))
            {
                Console.Error.WriteLine("RatingSystems.Init did not finish reading battles");
                return 1;
            }

            var casual = RatingSystems.GetRatingSystem(RatingCategory.Casual);

            if (!Wait(() => RatingPipeline.FullPassFinished(casual), TimeSpan.FromMinutes(5)))
            {
                Console.Error.WriteLine("the rating computation did not finish");
                return 1;
            }

            List<int> accounts;
            using (var db = new ZkDataContext()) accounts = db.Accounts.Select(a => a.AccountID).ToList();
            accounts.Sort();

            var lines = WaitForStableRatings(() => Snapshot(casual, accounts), TimeSpan.FromMinutes(3));
            if (lines == null)
            {
                Console.Error.WriteLine("the ratings were still moving after 3 minutes");
                return 1;
            }

            Console.Error.WriteLine("converged in " + clock.Elapsed.TotalSeconds.ToString("0.0")
                                    + "s on .NET Framework " + Environment.Version + ", " + battles + " battles");

            if (string.IsNullOrEmpty(outputPath)) foreach (var line in lines) Console.WriteLine(line);
            else File.WriteAllLines(outputPath, lines);
            Console.Error.WriteLine("wrote " + (lines.Count - 1) + " accounts"
                                    + (string.IsNullOrEmpty(outputPath) ? "" : " to " + outputPath));
            return 0;
        }

        private static List<string> Snapshot(IRatingSystem system, IEnumerable<int> accounts)
        {
            var lines = new List<string> { "accountID\trealElo\teloStdev\tlastGameDate\trank" };
            foreach (var account in accounts)
            {
                var rating = system.GetPlayerRating(account);
                lines.Add(string.Join("\t", new[]
                {
                    account.ToString(CultureInfo.InvariantCulture),

                    // G9, not F6: this runtime's fixed-point formatting of a float carries
                    // about seven significant digits and pads the rest with zeros, so
                    // 1458.3042f prints as 1458.304000 here and 1458.304199 on .NET 9 - a
                    // difference in the PRINTING that would masquerade as a difference in the
                    // rating. G9 round-trips a float exactly on both runtimes.
                    rating.RealElo.ToString("G9", CultureInfo.InvariantCulture),
                    rating.EloStdev.ToString("G9", CultureInfo.InvariantCulture),
                    rating.LastGameDate.ToString(CultureInfo.InvariantCulture),
                    rating.Rank.ToString(CultureInfo.InvariantCulture),
                }));
            }
            return lines;
        }

        private static List<string> WaitForStableRatings(Func<List<string>> snapshot, TimeSpan timeout)
        {
            string previous = null;
            var repeats = 0;
            var until = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < until)
            {
                var lines = snapshot();
                var signature = string.Join("|", lines);
                if (signature == previous)
                {
                    if (++repeats >= 2) return lines;
                }
                else
                {
                    repeats = 0;
                    previous = signature;
                }
                Thread.Sleep(300);
            }
            return null;
        }

        private static bool Wait(Func<bool> condition, TimeSpan timeout)
        {
            var until = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < until)
            {
                bool ok;
                try { ok = condition(); }
                catch { ok = false; }
                if (ok) return true;
                Thread.Sleep(250);
            }
            return false;
        }
    }
}
