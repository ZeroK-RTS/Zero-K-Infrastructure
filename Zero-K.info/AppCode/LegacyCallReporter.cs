using System;
using System.Collections.Concurrent;

namespace ZeroKWeb
{
    /// <summary>
    /// Counts calls to a deprecated endpoint and decides when one is worth a trace line.
    ///
    /// It exists because the retirement of ContentService.svc needs two numbers this repository
    /// cannot otherwise see - is anyone still calling, and how much - and because the obvious way
    /// to get them is not affordable. ZkServerTraceListener turns every trace into its own
    /// ZkDataContext and one LogEntries insert, and this endpoint's traffic is unknown by
    /// definition, so a line per call would put a database write on a path that has none today.
    ///
    /// So: the FIRST call for a given operation and client reports immediately - "no line" means
    /// "no caller", which is the answer the retirement actually turns on - and after that at most
    /// one line per <see cref="reportEvery"/>, carrying the count since the previous one.
    ///
    /// Pure and clock-injected so Tests.Portable can drive it; it deliberately knows nothing
    /// about WCF or tracing, and returns the line rather than writing it. ContentService.svc.cs
    /// is the caller.
    /// </summary>
    public class LegacyCallReporter
    {
        readonly string endpoint;
        readonly TimeSpan reportEvery;
        readonly int maxTrackedClients;
        readonly Func<DateTime> clock;

        readonly ConcurrentDictionary<string, Window> windows = new ConcurrentDictionary<string, Window>();

        /// <param name="endpoint">Names the endpoint in every line, so one query finds them all.</param>
        /// <param name="maxTrackedClients">
        /// A ceiling on distinct operation-and-client keys, so a caller sending a varying user
        /// agent cannot grow this without limit. Keys past the ceiling are counted together under
        /// one bucket rather than dropped, because a call that is not counted is indistinguishable
        /// from an endpoint nobody uses - which is the one conclusion this must not invent.
        /// </param>
        public LegacyCallReporter(string endpoint, TimeSpan reportEvery, int maxTrackedClients, Func<DateTime> clock = null)
        {
            if (string.IsNullOrEmpty(endpoint)) throw new ArgumentException("endpoint is what the log line is searched by", "endpoint");
            if (reportEvery <= TimeSpan.Zero) throw new ArgumentException("a zero or negative window would report every call", "reportEvery");
            if (maxTrackedClients < 1) throw new ArgumentException("at least one client has to be countable", "maxTrackedClients");

            this.endpoint = endpoint;
            this.reportEvery = reportEvery;
            this.maxTrackedClients = maxTrackedClients;
            this.clock = clock ?? (() => DateTime.UtcNow);
        }

        /// <summary>The number of distinct operation-and-client keys being counted. For tests.</summary>
        public int TrackedClients { get { return windows.Count; } }

        /// <summary>
        /// Records one call and returns the line to log, or null when this call falls inside a
        /// window that has already reported.
        /// </summary>
        public string Record(string operation, string client, string caller = null, string address = null, string detail = null)
        {
            if (operation == null) throw new ArgumentNullException("operation");
            if (string.IsNullOrEmpty(client)) client = "user agent not sent";

            // Operation x client. Deliberately NOT the address: that is one key per calling
            // machine, which is the unbounded growth maxTrackedClients exists to prevent.
            var key = operation + " | " + client;
            if (!windows.ContainsKey(key) && windows.Count >= maxTrackedClients)
                key = operation + " | (more clients than this counts)";

            var window = windows.GetOrAdd(key, _ => new Window(clock()));

            lock (window)
            {
                var now = clock();
                window.Count++;
                if (caller != null) window.LastCaller = caller;
                if (detail != null) window.LastDetail = detail;
                if (address != null) window.LastAddress = address;

                var elapsed = now - window.Start;
                if (window.EverReported && elapsed < reportEvery) return null;

                var line = string.Format("{0} (deprecated): {1}, {2}, by {3} from {4}, client {5}{6}",
                    endpoint,
                    operation,
                    window.EverReported
                        ? string.Format("{0} calls in the last {1:N0} minutes", window.Count, elapsed.TotalMinutes)
                        : "first call seen",
                    window.LastCaller ?? "anonymous",
                    window.LastAddress ?? "address unknown",
                    client,
                    window.LastDetail != null ? ", " + window.LastDetail : "");

                window.EverReported = true;
                window.Count = 0;
                window.Start = now;
                return line;
            }
        }

        class Window
        {
            public Window(DateTime start) { Start = start; }

            public long Count;
            public DateTime Start;
            public bool EverReported;
            public string LastCaller;
            public string LastAddress;
            public string LastDetail;
        }
    }
}
