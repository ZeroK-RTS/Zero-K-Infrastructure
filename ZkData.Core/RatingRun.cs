using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using PlasmaShared;
using Ratings;
using ZkData;

namespace ZkData.Core
{
    /// <summary>
    /// Runs the real rating pipeline on .NET 9 and writes what it produced, so the same run
    /// on .NET Framework can be compared against it line for line.
    ///
    ///     ZK_CONNECTION_STRING=...zk_test... dotnet run -- rate out.tsv
    ///
    /// This is the check §7 of the modernization plan asks for by name. Schema equality says
    /// the two models describe the same database; it says nothing about whether EF Core's
    /// LINQ translation returns the same ROWS IN THE SAME ORDER as EF6's. Whole History
    /// Rating is iterative and order-sensitive, so a query that quietly changed shows up here
    /// as a number that moved, and nowhere else.
    ///
    /// Tests.Database/RatingDump.cs is the other half, and the two are deliberately written
    /// the same way step for step: the comparison is only worth anything if the only
    /// difference between the runs is the data layer underneath.
    /// </summary>
    public static class RatingRun
    {
        public static int Run(ZkDataContext db, string outputPath)
        {
            // The fixture carries the live pipeline's stored ratings. Left in place,
            // GetPlayerRating answers from that cache and the computation never runs.
            db.Database.ExecuteSqlRaw("DELETE FROM AccountRatings");

            var battles = db.SpringBattles.AsNoTracking().Count();
            if (battles == 0)
            {
                Console.Error.WriteLine("no battles in this database - load the fixture first");
                return 2;
            }

            var clock = Stopwatch.StartNew();
            RatingSystems.Init();

            if (!Wait(() => RatingSystems.Initialized, TimeSpan.FromMinutes(3)))
            {
                Console.Error.WriteLine("RatingSystems.Init did not finish reading battles");
                return 1;
            }

            var casual = RatingSystems.GetRatingSystem(RatingCategory.Casual);

            // Deliberately NOT ForceRatingsUpdate(). Init() already queues the full
            // 150-iteration pass; forcing a second one makes the number of iterations depend
            // on which task the scheduler got to first, and the two stacks then disagree by
            // about 1e-4 at random. That is scheduling noise wearing the costume of a
            // porting bug - the first version of this harness reported it as one.
            if (!Wait(() => FullPassFinished(casual), TimeSpan.FromMinutes(5)))
            {
                Console.Error.WriteLine("the rating computation did not finish");
                return 1;
            }

            var accounts = db.Accounts.AsNoTracking().Select(a => a.AccountID).ToList();
            accounts.Sort();

            // The battle guard above does not cover this one: battles with no accounts to rate
            // writes a file holding nothing but a header, and db/compare-ratings.sh then diffs it
            // against an equally empty EF6 dump and reports the two stacks agreeing. An export of
            // nothing is not a result, so it is refused here as well as compared there.
            if (accounts.Count == 0)
            {
                Console.Error.WriteLine("no accounts in this database - there would be nothing to rate");
                return 2;
            }

            var lines = WaitForStableRatings(() => Snapshot(casual, accounts), TimeSpan.FromMinutes(3));
            if (lines == null)
            {
                Console.Error.WriteLine("the ratings were still moving after 3 minutes");
                return 1;
            }

            Console.Error.WriteLine("converged in " + clock.Elapsed.TotalSeconds.ToString("0.0")
                                    + "s on .NET " + Environment.Version + ", " + battles + " battles");

            if (string.IsNullOrEmpty(outputPath)) foreach (var line in lines) Console.WriteLine(line);
            else File.WriteAllLines(outputPath, lines);
            Console.Error.WriteLine("wrote " + (lines.Count - 1) + " accounts"
                                    + (string.IsNullOrEmpty(outputPath) ? "" : " to " + outputPath));
            return 0;
        }

        /// <summary>
        /// WholeHistoryRating sets <c>completelyInitialized</c> after runIterations(150) and
        /// the ranking pass, and exposes no public equivalent - the work happens on a
        /// background task behind a private static lock. Reading the field is how a harness
        /// asks "is it done?" without changing the production class for the benefit of the
        /// test. The alternative, sampling until the numbers stop moving, answers too early:
        /// they are perfectly still before the task starts.
        /// </summary>
        private static bool FullPassFinished(IRatingSystem system)
        {
            var whr = system as WholeHistoryRating;
            if (whr == null) return true;
            var field = typeof(WholeHistoryRating).GetField("completelyInitialized",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return field != null && (bool)field.GetValue(whr);
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

                    // G9, not F6: .NET Framework's fixed-point formatting of a float carries
                    // about seven significant digits and pads the rest with zeros, so
                    // 1458.3042f prints as 1458.304000 there and 1458.304199 on .NET 9 - a
                    // difference in the PRINTING that would masquerade as a difference in the
                    // rating. G9 round-trips a float exactly on both runtimes, so equal
                    // strings mean equal bits.
                    rating.RealElo.ToString("G9", CultureInfo.InvariantCulture),
                    rating.EloStdev.ToString("G9", CultureInfo.InvariantCulture),
                    rating.LastGameDate.ToString(CultureInfo.InvariantCulture),
                    rating.Rank.ToString(CultureInfo.InvariantCulture),
                }));
            }
            return lines;
        }

        /// <summary>Belt and braces after the completion flag: the ranking pass writes through
        /// collections the reader also walks, so one settled sample is not proof of two.</summary>
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
