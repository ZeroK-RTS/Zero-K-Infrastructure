using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ZeroKWeb
{
    /// <summary>One row of LogEntries, without depending on the entity.</summary>
    public struct LegacyLogLine
    {
        public LegacyLogLine(DateTime time, string message) { Time = time; Message = message; }
        public DateTime Time { get; }
        public string Message { get; }
    }

    public enum LegacyEndpointVerdict
    {
        /// <summary>Nothing in the window says an instrumented build was running. Silence means nothing.</summary>
        NoWatchSeen,

        /// <summary>An instrumented build reported for itself, and nobody called this endpoint.</summary>
        NoCalls,

        /// <summary>Somebody is still calling it.</summary>
        StillCalled,
    }

    public class LegacyEndpointFinding
    {
        public string Endpoint;
        public LegacyEndpointVerdict Verdict;
        public int Lines;
        public DateTime? LastCall;
        public DateTime? WatchSince;
        public List<string> EditorVersions = new List<string>();
        public List<string> Examples = new List<string>();
    }

    /// <summary>
    /// Reads what the two deprecated WCF endpoints said about themselves, and answers the only
    /// question the retirement turns on.
    ///
    /// **The trap this exists to close is that silence has two meanings.** `MissionService.svc` and
    /// `ContentService.svc` report their own callers into LogEntries, so the plan was that somebody
    /// would look and see nothing. But seeing nothing is also what a build without the
    /// instrumentation looks like, and what a site that has not been deployed looks like, and what
    /// a broken trace listener looks like. Retiring a live endpoint on that reading would break
    /// publishing for every mission editor that has not been updated.
    ///
    /// So a watched build says so at startup - <see cref="WatchMarker"/>, written by
    /// Global.StartApplication next to the listener it depends on - and this refuses to draw any
    /// conclusion from a window that does not contain one. "No calls" is only ever reported
    /// relative to a time when the reporting is known to have been running.
    ///
    /// It is pure so Tests.Portable can drive it; ZkData.Core supplies the rows.
    /// </summary>
    public static class LegacyCallAudit
    {
        /// <summary>What Global.StartApplication writes when this build carries the reporting.</summary>
        public const string WatchMarker = "legacy WCF watch active";

        private static readonly Regex EditorVersion =
            new Regex(@"mission editor (?<version>[^\s,]+)", RegexOptions.IgnoreCase);

        public static List<LegacyEndpointFinding> Audit(IEnumerable<LegacyLogLine> lines,
                                                        IEnumerable<string> endpoints,
                                                        DateTime now,
                                                        TimeSpan window)
        {
            var since = now - window;
            var inWindow = (lines ?? Enumerable.Empty<LegacyLogLine>())
                .Where(x => x.Message != null && x.Time >= since)
                .OrderBy(x => x.Time)
                .ToList();

            DateTime? watchSince = inWindow
                .Where(x => x.Message.IndexOf(WatchMarker, StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(x => (DateTime?)x.Time)
                .FirstOrDefault();

            var findings = new List<LegacyEndpointFinding>();
            foreach (var endpoint in endpoints)
            {
                // Both reporters write "<endpoint> (deprecated" - one says "(deprecated WCF
                // endpoint)", the other "(deprecated)". Matching the common prefix keeps this from
                // depending on wording that is not load-bearing.
                var marker = endpoint + " (deprecated";
                var calls = inWindow
                    .Where(x => x.Message.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

                var finding = new LegacyEndpointFinding
                {
                    Endpoint = endpoint,
                    Lines = calls.Count,
                    LastCall = calls.Count > 0 ? (DateTime?)calls[calls.Count - 1].Time : null,
                    WatchSince = watchSince,
                };

                if (calls.Count > 0) finding.Verdict = LegacyEndpointVerdict.StillCalled;
                else if (watchSince == null) finding.Verdict = LegacyEndpointVerdict.NoWatchSeen;
                else finding.Verdict = LegacyEndpointVerdict.NoCalls;

                // The editor version is the number MissionService's retirement turns on, so it is
                // pulled out rather than left for a human to read off the lines.
                finding.EditorVersions = calls
                    .Select(x => EditorVersion.Match(x.Message))
                    .Where(m => m.Success)
                    .Select(m => m.Groups["version"].Value)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                finding.Examples = calls.Select(x => x.Message).Reverse().Take(5).ToList();
                findings.Add(finding);
            }
            return findings;
        }

        /// <summary>The sentence a person acts on. Deliberately says what cannot be concluded.</summary>
        public static string Describe(LegacyEndpointFinding finding, TimeSpan window)
        {
            switch (finding.Verdict)
            {
                case LegacyEndpointVerdict.NoWatchSeen:
                    return string.Format(
                        "{0}: CANNOT TELL. No \"{1}\" in the last {2:N0} days, so nothing says an "
                        + "instrumented build was running. Silence here is not evidence - deploy and wait.",
                        finding.Endpoint, WatchMarker, window.TotalDays);

                case LegacyEndpointVerdict.NoCalls:
                    return string.Format(
                        "{0}: no calls since {1:yyyy-MM-dd HH:mm} UTC, when a watched build last started. "
                        + "That is the evidence the retirement needs.",
                        finding.Endpoint, finding.WatchSince);

                default:
                    return string.Format(
                        "{0}: STILL CALLED - {1} report line(s), most recently {2:yyyy-MM-dd HH:mm} UTC.{3}",
                        finding.Endpoint, finding.Lines, finding.LastCall,
                        finding.EditorVersions.Count > 0
                            ? " Mission editor versions seen: " + string.Join(", ", finding.EditorVersions) + "."
                            : "");
            }
        }
    }
}
