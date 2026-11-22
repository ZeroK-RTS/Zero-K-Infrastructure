using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZeroKWeb;

namespace Tests.Portable
{
    /// <summary>
    /// The rule these pin is one sentence: an endpoint nobody calls and an endpoint nobody is
    /// watching look identical in a log, and only one of them may be deleted.
    ///
    /// Getting that backwards deletes an endpoint that mission editors still publish through, and
    /// the log shows nothing either way, so it is not a mistake that announces itself.
    /// </summary>
    [TestClass]
    public class LegacyCallAuditTests
    {
        private static readonly DateTime Now = new DateTime(2026, 9, 27, 12, 0, 0, DateTimeKind.Utc);
        private static readonly TimeSpan Window = TimeSpan.FromDays(14);
        private static readonly string[] Both = { "MissionService.svc", "ContentService.svc" };

        private static LegacyLogLine Line(double daysAgo, string message)
            => new LegacyLogLine(Now.AddDays(-daysAgo), message);

        private static LegacyEndpointFinding For(IEnumerable<LegacyLogLine> lines, string endpoint)
            => LegacyCallAudit.Audit(lines, Both, Now, Window).Single(x => x.Endpoint == endpoint);

        private static LegacyLogLine Watch(double daysAgo)
            => Line(daysAgo, LegacyCallAudit.WatchMarker + ": MissionService.svc and ContentService.svc report their callers");

        [TestMethod]
        public void An_empty_log_says_nothing_either_way()
        {
            var finding = For(new LegacyLogLine[0], "MissionService.svc");
            Assert.AreEqual(LegacyEndpointVerdict.NoWatchSeen, finding.Verdict,
                "with no watch line, silence is not evidence of anything");
            StringAssert.Contains(LegacyCallAudit.Describe(finding, Window), "CANNOT TELL");
        }

        [TestMethod]
        public void Callers_but_no_watch_line_is_still_called()
        {
            // The reporting can only produce a call line by running, so this cannot be "cannot
            // tell" - a caller is a caller however the window began.
            var finding = For(new[] { Line(2, "ContentService.svc (deprecated): DownloadFile, first call seen, by anonymous from 1.2.3.4, client Spring") },
                              "ContentService.svc");
            Assert.AreEqual(LegacyEndpointVerdict.StillCalled, finding.Verdict);
        }

        [TestMethod]
        public void A_watched_window_with_no_calls_is_the_evidence_to_retire_on()
        {
            var finding = For(new[] { Watch(10) }, "ContentService.svc");
            Assert.AreEqual(LegacyEndpointVerdict.NoCalls, finding.Verdict);
            Assert.AreEqual(Now.AddDays(-10), finding.WatchSince);
            StringAssert.Contains(LegacyCallAudit.Describe(finding, Window), "no calls since");
        }

        [TestMethod]
        public void The_watch_that_counts_is_the_earliest_in_the_window()
        {
            // Restarts are ordinary. "No calls since the LAST restart" would throw away most of
            // the evidence and could claim safety from a window minutes long.
            var finding = For(new[] { Watch(9), Watch(4), Watch(1) }, "MissionService.svc");
            Assert.AreEqual(Now.AddDays(-9), finding.WatchSince);
        }

        [TestMethod]
        public void Anything_older_than_the_window_is_not_looked_at()
        {
            var finding = For(new[] { Watch(20), Line(18, "MissionService.svc (deprecated WCF endpoint): SendMission by bob, mission editor 1.2.3") },
                              "MissionService.svc");
            Assert.AreEqual(LegacyEndpointVerdict.NoWatchSeen, finding.Verdict,
                "a watch line from before the window cannot vouch for silence inside it");
            Assert.AreEqual(0, finding.Lines);
        }

        [TestMethod]
        public void One_endpoint_being_called_says_nothing_about_the_other()
        {
            var lines = new[]
            {
                Watch(7),
                Line(1, "ContentService.svc (deprecated): DownloadFile, first call seen, by anonymous from 1.2.3.4, client Spring"),
            };
            Assert.AreEqual(LegacyEndpointVerdict.StillCalled, For(lines, "ContentService.svc").Verdict);
            Assert.AreEqual(LegacyEndpointVerdict.NoCalls, For(lines, "MissionService.svc").Verdict,
                "they are retired separately and the log distinguishes them");
        }

        [TestMethod]
        public void Mission_editor_versions_are_collected_because_the_retirement_turns_on_them()
        {
            var lines = new[]
            {
                Watch(7),
                Line(3, "MissionService.svc (deprecated WCF endpoint): SendMission by bob, mission editor 1.2.3"),
                Line(2, "MissionService.svc (deprecated WCF endpoint): SendMission by ann, mission editor 1.4.0"),
                Line(1, "MissionService.svc (deprecated WCF endpoint): SendMission by bob, mission editor 1.2.3"),
            };
            var finding = For(lines, "MissionService.svc");
            Assert.AreEqual(3, finding.Lines);
            CollectionAssert.AreEqual(new[] { "1.2.3", "1.4.0" }, finding.EditorVersions,
                "distinct and sorted, so 'has everyone updated' is readable at a glance");
            StringAssert.Contains(LegacyCallAudit.Describe(finding, Window), "1.4.0");
        }

        [TestMethod]
        public void Both_reporters_phrase_the_marker_differently_and_both_are_matched()
        {
            // One writes "(deprecated WCF endpoint)", the other "(deprecated)". The audit matches
            // the common prefix rather than either wording.
            var lines = new[]
            {
                Watch(5),
                Line(2, "MissionService.svc (deprecated WCF endpoint): GetMission by anonymous, mission editor version not reported"),
                Line(2, "ContentService.svc (deprecated): GetResourceData, 4 calls in the last 60 minutes, by anonymous from 5.6.7.8, client zk"),
            };
            Assert.AreEqual(LegacyEndpointVerdict.StillCalled, For(lines, "MissionService.svc").Verdict);
            Assert.AreEqual(LegacyEndpointVerdict.StillCalled, For(lines, "ContentService.svc").Verdict);
        }
    }
}
