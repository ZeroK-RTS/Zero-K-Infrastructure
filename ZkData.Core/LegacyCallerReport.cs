using System;
using System.Linq;
using ZeroKWeb;

namespace ZkData.Core
{
    /// <summary>
    /// Answers whether the two deprecated WCF endpoints can be deleted yet, from LogEntries.
    ///
    ///     ZK_CONNECTION_STRING=... dotnet run --project ZkData.Core -- legacy-callers [--days=N]
    ///
    /// The plan for retiring `MissionService.svc` and `ContentService.svc` was that they would
    /// report their own callers and somebody would look. This is the looking, done the same way
    /// twice instead of by eye, and it exists mostly to make one distinction that is easy to miss:
    /// **an endpoint nobody calls and an endpoint nobody is watching look identical in a log.**
    ///
    /// So it reports CANNOT TELL unless the window contains a startup line from a build that
    /// carries the instrumentation. See <see cref="LegacyCallAudit"/>.
    ///
    /// Read-only. It runs against production by pointing ZK_CONNECTION_STRING at it, and the
    /// 14 days LogEntries keeps is the longest window worth asking for.
    /// </summary>
    public static class LegacyCallerReport
    {
        private static readonly string[] Endpoints = { "MissionService.svc", "ContentService.svc" };

        public static int Run(ZkDataContext db, string[] arguments)
        {
            var days = 14;
            var daysArg = arguments.FirstOrDefault(x => x.StartsWith("--days="));
            if (daysArg != null && !int.TryParse(daysArg.Substring("--days=".Length), out days))
            {
                Console.Error.WriteLine("--days wants a number, as in --days=7");
                return 2;
            }

            var window = TimeSpan.FromDays(days);
            var now = DateTime.UtcNow;
            var since = now - window;

            // Filtered in SQL: LogEntries is every Trace the site and the lobby server make, and
            // pulling 14 days of it back to filter here would be silly.
            var rows = db.LogEntries
                .Where(x => x.Time >= since)
                .Where(x => x.Message.Contains("(deprecated") || x.Message.Contains(LegacyCallAudit.WatchMarker))
                .OrderBy(x => x.Time)
                .Select(x => new { x.Time, x.Message })
                .ToList()
                .Select(x => new LegacyLogLine(x.Time, x.Message));

            var findings = LegacyCallAudit.Audit(rows, Endpoints, now, window);

            Console.WriteLine();
            Console.WriteLine("legacy WCF endpoints, over the last {0} day(s) of LogEntries:", days);
            Console.WriteLine();

            var retirable = 0;
            foreach (var finding in findings)
            {
                Console.WriteLine("  " + LegacyCallAudit.Describe(finding, window));
                foreach (var example in finding.Examples) Console.WriteLine("      " + example);
                Console.WriteLine();
                if (finding.Verdict == LegacyEndpointVerdict.NoCalls) retirable++;
            }

            if (retirable == findings.Count)
            {
                Console.WriteLine("Both endpoints are unused over this window. Deleting the .svc files,");
                Console.WriteLine("their code-behind and their Web.config entries is the remaining work.");
            }
            else if (findings.Any(x => x.Verdict == LegacyEndpointVerdict.NoWatchSeen))
            {
                Console.WriteLine("Nothing can be concluded yet. Deploy a build carrying the watch line and");
                Console.WriteLine("come back - LogEntries keeps 14 days, so ask again inside that.");
            }

            return 0;
        }
    }
}
