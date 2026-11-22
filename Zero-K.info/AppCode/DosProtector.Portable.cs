using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace ZeroKWeb
{
    /// <summary>
    /// The site's rate limiter, as arithmetic over request timings and nothing else.
    ///
    /// This is the whole of what DosProtector did; what is left in DosProtector.cs is the MVC 5
    /// half that pulls an address and a path out of a System.Web.HttpRequest and hands them here.
    /// Split because System.Web is the one thing that cannot cross to .NET 9, and because two
    /// copies of a rate limiter would drift - the port compiles THIS file, linked, rather than a
    /// twin of it.
    ///
    /// Two limits, per address, both from the original and neither of them changed:
    ///
    ///   * more than 15 requests in flight at once, or
    ///   * more than 11 seconds of request time inside the last 5 seconds.
    ///
    /// The second is the one that matters, and it is why this counts SECONDS rather than requests:
    /// a caller asking for cheap pages as fast as it can is not the problem, and a caller holding
    /// open the pages that take the database a second each is. Eleven against a five-second window
    /// means roughly two concurrent slow requests sustained, which is more than a person browsing
    /// and less than a scraper.
    /// </summary>
    public partial class DosProtector
    {
        private long requestCounter;

        private readonly ConcurrentDictionary<long, DosEntry> requests =
            new ConcurrentDictionary<long, DosEntry>();

        public const int MaxParallel = 15;
        public const double MaxSecondsInWindow = 11;
        public static readonly TimeSpan Window = TimeSpan.FromSeconds(5);

        private readonly Func<DateTime> utcNow;

        /// <summary>
        /// The clock is injectable for the same reason SessionTokenStore's is: the window is five
        /// seconds, and a test that proves the SECOND limit by sleeping through it is a test that
        /// is slow and occasionally wrong. Everything in production uses the default.
        /// </summary>
        public DosProtector(Func<DateTime> utcNow = null)
        {
            this.utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        /// <summary>Whether this address may be served right now.</summary>
        public bool CanQuery(string ip)
        {
            // No address, no accounting. On MVC 5 this came from Request.UserHostAddress and on
            // ASP.NET Core from the connection; both can be null, and refusing a request because
            // its address is unknown would be a worse failure than letting it through.
            if (string.IsNullOrEmpty(ip)) return true;

            var now = utcNow();
            var limit = now - Window;

            var parallel = requests.Values.Count(x => x != null && x.IP == ip && x.RequestEnd > now);
            if (parallel > MaxParallel) return false;

            // Each entry contributes the part of itself that falls INSIDE the window: a request
            // that started before it counts from the window's edge, and one still running counts
            // up to now rather than to its end, which it does not have yet.
            var totalTime = requests.Values
                .Where(x => x != null && x.IP == ip && x.RequestEnd >= limit)
                .Select(x => (x.RequestEnd < now ? x.RequestEnd : now)
                             .Subtract(x.RequestStart > limit ? x.RequestStart : limit).TotalSeconds)
                .Sum();

            return totalTime <= MaxSecondsInWindow;
        }

        /// <summary>Starts accounting for a request, and returns the id that ends it.</summary>
        public long RequestStart(string ip)
        {
            var requestID = Interlocked.Increment(ref requestCounter);
            requests[requestID] = new DosEntry
            {
                RequestID = requestID,
                RequestStart = utcNow(),
                IP = ip,
            };
            return requestID;
        }

        /// <summary>
        /// Ends it, and drops everything that has fallen out of the window. A null id is a request
        /// that was never started - an exempt one, or one refused before it began - and is ignored,
        /// which is what the MVC 5 version did by finding nothing in HttpContext.Items.
        /// </summary>
        public void RequestEnd(long? requestID)
        {
            if (requestID == null) return;

            DosEntry entry;
            if (requests.TryGetValue(requestID.Value, out entry)) entry.RequestEnd = utcNow();

            var limit = utcNow() - Window;
            foreach (var old in requests.Values.Where(x => x != null && x.RequestEnd < limit).ToList())
            {
                DosEntry removed;
                requests.TryRemove(old.RequestID, out removed);
            }
        }

        /// <summary>How many requests are being accounted for. For tests and for a status page.</summary>
        public int Tracked => requests.Count;

        public class DosEntry
        {
            public string IP;
            public DateTime RequestEnd = DateTime.MaxValue;
            public long RequestID;
            public DateTime RequestStart;
        }
    }
}
