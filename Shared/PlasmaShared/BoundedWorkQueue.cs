using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlasmaShared
{
    /// <summary>
    /// A queue of keys to work through in the background, never more than a few at a time, each
    /// key tried once.
    ///
    /// Written for LegacyMinimapUpdater, which is handed a map name on every map view and must
    /// correct that map's stored images at most once, off the request thread, without letting a
    /// burst of views turn into a burst of archive downloads. None of that is about maps, so none
    /// of it is in there: this is the part with logic worth testing, and it is here because here
    /// it compiles on .NET 9, which is where the tests run.
    ///
    /// **The failure policy is deliberately blunt: one failure stops the queue for good.** The
    /// caller's work either can be done in this process or cannot - no registrar, no downloader,
    /// no native library - and when it cannot, every later key would fail the same way and say so
    /// in the log every time. Stopping is recoverable by a restart, which is when the thing that
    /// was missing might not be.
    /// </summary>
    public class BoundedWorkQueue
    {
        private readonly object locker = new object();
        private readonly Queue<string> pending = new Queue<string>();
        private readonly HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly int maxConcurrent;
        private readonly Action<string> work;
        private readonly Action<string, Exception> onFailure;
        private readonly Action<Action> start;
        private int active;
        private bool stopped;

        /// <param name="maxConcurrent">How many may be in flight. One failure stops all of them.</param>
        /// <param name="work">Does the work for one key. Throwing stops the queue.</param>
        /// <param name="onFailure">Told about the throw that stopped it, once.</param>
        /// <param name="start">
        /// How to run a worker; defaults to the thread pool. A test passes something that runs it
        /// when the test says so, which is what makes "two at a time" an assertion rather than a
        /// race - with Task.Run the first worker can finish before the second is even queued, and
        /// a test written against that would pass whatever the limit was set to.
        /// </param>
        public BoundedWorkQueue(int maxConcurrent, Action<string> work,
            Action<string, Exception> onFailure = null, Action<Action> start = null)
        {
            if (maxConcurrent < 1) throw new ArgumentOutOfRangeException("maxConcurrent");
            this.maxConcurrent = maxConcurrent;
            this.work = work ?? throw new ArgumentNullException("work");
            this.onFailure = onFailure;
            this.start = start ?? (body => Task.Run(body));
        }

        /// <summary>In flight right now. For tests and for a status page.</summary>
        public int Active { get { lock (locker) return active; } }

        /// <summary>Waiting for a free slot.</summary>
        public int Waiting { get { lock (locker) return pending.Count; } }

        /// <summary>True once a failure has stopped it.</summary>
        public bool Stopped { get { lock (locker) return stopped; } }

        /// <summary>
        /// Offers a key. Cheap and synchronous - a set lookup, and an enqueue the first time a key
        /// is seen. Everything else happens on a worker.
        /// </summary>
        public void Notice(string key)
        {
            if (string.IsNullOrEmpty(key)) return;

            lock (locker)
            {
                if (stopped) return;
                if (!seen.Add(key)) return;
                pending.Enqueue(key);
            }

            Pump();
        }

        private void Pump()
        {
            while (true)
            {
                string next;
                lock (locker)
                {
                    if (stopped || active >= maxConcurrent || pending.Count == 0) return;
                    next = pending.Dequeue();
                    active++;
                }

                start(() => Run(next));
            }
        }

        private void Run(string key)
        {
            try
            {
                work(key);
            }
            catch (Exception ex)
            {
                lock (locker)
                {
                    stopped = true;
                    pending.Clear();
                }
                if (onFailure != null) onFailure(key, ex);
            }
            finally
            {
                lock (locker) active--;
                Pump();
            }
        }
    }
}
