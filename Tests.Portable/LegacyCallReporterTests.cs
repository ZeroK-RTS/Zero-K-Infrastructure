using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZeroKWeb;

namespace Tests.Portable
{
    /// <summary>
    /// The counting behind ContentService.svc's deprecation notice. Its whole job is to make one
    /// question answerable from LogEntries - is anyone still calling the WCF endpoint, and how
    /// much - without putting a database write on every call, so the two failure modes that
    /// matter are reporting too much (a write per call) and reporting too little (silence read as
    /// "nobody is calling", which would retire an endpoint people still use).
    /// </summary>
    [TestClass]
    public class LegacyCallReporterTests
    {
        private DateTime now = new DateTime(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc);

        private LegacyCallReporter Reporter(int maxTrackedClients = 200)
        {
            return new LegacyCallReporter("ContentService.svc", TimeSpan.FromHours(1), maxTrackedClients, () => now);
        }

        [TestMethod]
        public void The_first_call_is_always_reported()
        {
            // The point of the whole class: no line must mean no caller.
            var line = Reporter().Record("DownloadFile", "ZeroKLobby/1.2.3");

            StringAssert.Contains(line, "ContentService.svc");
            StringAssert.Contains(line, "DownloadFile");
            StringAssert.Contains(line, "first call seen");
            StringAssert.Contains(line, "ZeroKLobby/1.2.3");
        }

        [TestMethod]
        public void Calls_inside_the_window_are_counted_but_not_reported()
        {
            var reporter = Reporter();
            Assert.IsNotNull(reporter.Record("DownloadFile", "old-client"));

            for (var i = 0; i < 500; i++)
            {
                now = now.AddSeconds(1);
                Assert.IsNull(reporter.Record("DownloadFile", "old-client"),
                    "a second line inside the window is a database write this exists to avoid");
            }
        }

        [TestMethod]
        public void The_next_window_reports_the_count_it_accumulated()
        {
            var reporter = Reporter();
            reporter.Record("DownloadFile", "old-client");

            for (var i = 0; i < 9; i++) reporter.Record("DownloadFile", "old-client");
            now = now.AddHours(1);

            var line = reporter.Record("DownloadFile", "old-client");
            // Nine silent calls plus this one; the reported first call is not counted again.
            StringAssert.Contains(line, "10 calls in the last 60 minutes");
        }

        [TestMethod]
        public void Counting_restarts_after_a_report()
        {
            var reporter = Reporter();
            reporter.Record("DownloadFile", "old-client");
            reporter.Record("DownloadFile", "old-client");
            now = now.AddHours(1);
            reporter.Record("DownloadFile", "old-client");

            now = now.AddHours(1);
            var line = reporter.Record("DownloadFile", "old-client");
            StringAssert.Contains(line, "1 calls in the last 60 minutes");
        }

        [TestMethod]
        public void Each_operation_and_client_is_counted_separately()
        {
            var reporter = Reporter();

            Assert.IsNotNull(reporter.Record("DownloadFile", "old-client"));
            Assert.IsNotNull(reporter.Record("GetResourceData", "old-client"),
                "a different operation is a different question");
            Assert.IsNotNull(reporter.Record("DownloadFile", "other-client"),
                "a different client is the whole point - it says who still has to update");
            Assert.IsNull(reporter.Record("DownloadFile", "old-client"));
            Assert.AreEqual(3, reporter.TrackedClients);
        }

        [TestMethod]
        public void A_varying_user_agent_cannot_grow_the_table_without_limit()
        {
            var reporter = Reporter(maxTrackedClients: 4);

            for (var i = 0; i < 1000; i++) reporter.Record("DownloadFile", "client-" + i);

            Assert.IsTrue(reporter.TrackedClients <= 5,
                "four tracked clients plus the one bucket the rest collapse into");
        }

        [TestMethod]
        public void Calls_past_the_ceiling_are_still_counted()
        {
            // Dropping them would make a busy endpoint look idle, which is the one
            // conclusion this must never invent.
            var reporter = Reporter(maxTrackedClients: 1);
            reporter.Record("DownloadFile", "first-client");
            reporter.Record("DownloadFile", "overflowing-client");

            for (var i = 0; i < 5; i++) reporter.Record("DownloadFile", "another-" + i);
            now = now.AddHours(1);

            var line = reporter.Record("DownloadFile", "yet-another");
            StringAssert.Contains(line, "6 calls in the last 60 minutes");
        }

        [TestMethod]
        public void The_reported_line_carries_the_last_login_address_and_detail_seen()
        {
            var reporter = Reporter();
            reporter.Record("SubmitMissionScore", "old-client", "firstuser", "10.0.0.1", "api version 3");
            reporter.Record("SubmitMissionScore", "old-client", "lateruser", "10.0.0.2", "api version 4");
            now = now.AddHours(1);

            var line = reporter.Record("SubmitMissionScore", "old-client");
            StringAssert.Contains(line, "lateruser");
            StringAssert.Contains(line, "10.0.0.2");
            StringAssert.Contains(line, "api version 4");
        }

        [TestMethod]
        public void A_caller_that_sends_nothing_identifying_still_reports()
        {
            // Every argument but the operation is optional, and WCF supplies none of them
            // when the request carries no User-Agent.
            var line = Reporter().Record("GetDefaultEngine", null);

            StringAssert.Contains(line, "user agent not sent");
            StringAssert.Contains(line, "anonymous");
            StringAssert.Contains(line, "address unknown");
        }

        [TestMethod]
        public void A_window_that_would_report_every_call_is_refused()
        {
            // The constructor's arguments are the two ways to turn this back into a write
            // per call, so both are refused rather than honoured.
            Assert.ThrowsException<ArgumentException>(
                () => new LegacyCallReporter("x", TimeSpan.Zero, 200));
            Assert.ThrowsException<ArgumentException>(
                () => new LegacyCallReporter("x", TimeSpan.FromHours(-1), 200));
        }
    }
}
