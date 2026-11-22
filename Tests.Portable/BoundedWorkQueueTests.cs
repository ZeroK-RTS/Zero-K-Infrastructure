using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlasmaShared;

namespace Tests
{
    /// <summary>
    /// The queue behind LegacyMinimapUpdater, which corrects a map's stored images the first time
    /// somebody opens that map. The map part needs unitsync and cannot be tested here; this part
    /// is the one with logic in it.
    ///
    /// Every test drives the workers by hand rather than through the thread pool. With Task.Run a
    /// worker can finish before the next key is even queued, so "never more than two at once" would
    /// pass against a queue with no limit at all - the assertion would be measuring the scheduler.
    /// </summary>
    [TestClass]
    public class BoundedWorkQueueTests
    {
        /// <summary>Collects workers instead of running them, so a test decides when each finishes.</summary>
        private sealed class Manual
        {
            public readonly List<Action> Started = new List<Action>();
            public void Start(Action body) => Started.Add(body);
            public void FinishOne()
            {
                var next = Started[0];
                Started.RemoveAt(0);
                next();
            }
        }

        [TestMethod]
        [TestCategory("Basic")]
        public void A_key_offered_twice_is_worked_once()
        {
            var done = new List<string>();
            var manual = new Manual();
            var queue = new BoundedWorkQueue(2, done.Add, null, manual.Start);

            queue.Notice("Small_Divide");
            queue.Notice("Small_Divide");
            queue.Notice("SMALL_DIVIDE");

            Assert.AreEqual(1, manual.Started.Count, "the same map queued three times started three workers");
            manual.FinishOne();
            CollectionAssert.AreEqual(new[] { "Small_Divide" }, done);
        }

        [TestMethod]
        [TestCategory("Basic")]
        public void Never_more_than_the_limit_are_in_flight()
        {
            var manual = new Manual();
            var queue = new BoundedWorkQueue(2, _ => { }, null, manual.Start);

            foreach (var name in new[] { "a", "b", "c", "d", "e" }) queue.Notice(name);

            Assert.AreEqual(2, queue.Active);
            Assert.AreEqual(2, manual.Started.Count, "a fifth map view started a third worker");
            Assert.AreEqual(3, queue.Waiting);
        }

        [TestMethod]
        [TestCategory("Basic")]
        public void A_worker_finishing_takes_the_next_one()
        {
            var done = new List<string>();
            var manual = new Manual();
            var queue = new BoundedWorkQueue(2, done.Add, null, manual.Start);

            foreach (var name in new[] { "a", "b", "c", "d" }) queue.Notice(name);
            Assert.AreEqual(2, queue.Waiting);

            manual.FinishOne();
            Assert.AreEqual(2, queue.Active, "a slot came free and nothing moved into it");
            Assert.AreEqual(1, queue.Waiting);

            while (manual.Started.Count > 0) manual.FinishOne();
            CollectionAssert.AreEqual(new[] { "a", "b", "c", "d" }, done, "worked out of order, or lost one");
            Assert.AreEqual(0, queue.Active);
        }

        [TestMethod]
        [TestCategory("Basic")]
        public void One_failure_stops_it_and_says_so_once()
        {
            var failures = new List<string>();
            var manual = new Manual();
            var queue = new BoundedWorkQueue(2,
                key => { if (key == "b") throw new InvalidOperationException("no unitsync here"); },
                (key, ex) => failures.Add(key + ": " + ex.Message),
                manual.Start);

            foreach (var name in new[] { "a", "b", "c", "d" }) queue.Notice(name);
            manual.FinishOne();   // a, fine
            manual.FinishOne();   // b, throws

            Assert.IsTrue(queue.Stopped);
            Assert.AreEqual(1, failures.Count);
            StringAssert.Contains(failures[0], "no unitsync here");
            Assert.AreEqual(0, queue.Waiting, "the queue was stopped and still has work in it");

            // Not "nothing is running": the worker for "c" was started BEFORE the failure and is
            // legitimately still in flight. What stopping means is that nothing NEW is taken on.
            var inFlight = manual.Started.Count;
            queue.Notice("e");
            Assert.AreEqual(inFlight, manual.Started.Count, "a stopped queue took more work");
        }

        [TestMethod]
        [TestCategory("Basic")]
        public void Work_sees_the_state_at_start_not_at_enqueue()
        {
            // This is what lets LegacyMinimapUpdater ask "is this map STILL legacy?" inside the
            // worker and trust the answer. The body runs when a slot comes free, not when the key
            // was offered, and between those two moments the map may have been corrected by the
            // other worker, by a bulk backfill-minimaps run, or by somebody pressing the admin
            // button. A queue that bound its work to the state at enqueue time would re-register a
            // map that no longer needed it.
            var alreadyDone = new HashSet<string>();
            var skipped = new List<string>();
            var manual = new Manual();
            var queue = new BoundedWorkQueue(1,
                key => { if (alreadyDone.Contains(key)) skipped.Add(key); },
                null, manual.Start);

            queue.Notice("Small_Divide");
            queue.Notice("Tabula");                 // waits behind it, the limit is one
            alreadyDone.Add("Tabula");              // corrected by something else while it waits

            manual.FinishOne();                     // Small_Divide runs, Tabula starts
            manual.FinishOne();                     // Tabula runs - against the CURRENT state

            CollectionAssert.AreEqual(new[] { "Tabula" }, skipped,
                "the worker did not see a change made after its key was queued");
        }

        [TestMethod]
        [TestCategory("Basic")]
        public void A_failure_does_not_leak_the_slot_it_was_using()
        {
            var manual = new Manual();
            var queue = new BoundedWorkQueue(2, _ => throw new Exception("boom"), null, manual.Start);

            queue.Notice("a");
            manual.FinishOne();

            Assert.AreEqual(0, queue.Active, "the worker threw and its slot was never given back");
        }
    }
}
