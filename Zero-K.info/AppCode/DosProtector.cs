using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;

namespace ZeroKWeb
{
    public class DosProtector
    {
        private long requestCounter;

        private ConcurrentDictionary<long, DosEntry> requests = new ConcurrentDictionary<long, DosEntry>();


        public bool CanQuery(HttpRequest request)
        {
            if (request.RequestContext.HttpContext.Handler == null) return true; // static files are allowed

            var ip = request.UserHostAddress;
            var now = DateTime.UtcNow;
            var limit = now.AddSeconds(-5);
            var parallel = requests.Values.Count(x => x != null && x.IP == ip && x.RequestEnd > now);
            if (parallel > 15) return false;

            var totalTime =
                requests.Values.Where(x => (x != null) && (x.IP == ip) && (x.RequestEnd >= limit))
                    .Select(x => x.RequestEnd < now ? x.RequestEnd.Subtract(x.RequestStart > limit ? x.RequestStart : limit).TotalSeconds : now.Subtract(x.RequestStart > limit ? x.RequestStart : limit).TotalSeconds)
                    .Sum();
            if (totalTime > 7.1) return false;
            return true;
        }


        public void RequestEnd(HttpRequest request)
        {
            var requestID = request.RequestContext.HttpContext.Items["requestID"] as long?;
            if (requestID.HasValue)
            {
                DosEntry entry;
                if (requests.TryGetValue(requestID.Value, out entry)) entry.RequestEnd = DateTime.UtcNow;
                var limit = DateTime.UtcNow.AddSeconds(-5);
                var toDel = requests.Values.Where(x => (x != null) && (x.RequestEnd < limit)).ToList();

                DosEntry dummy;
                foreach (var td in toDel) requests.TryRemove(td.RequestID, out dummy);
            }
        }

        public void RequestStart(HttpRequest request)
        {
            var ip = request.UserHostAddress;
            var requestID = Interlocked.Increment(ref requestCounter);
            requests[requestID] = new DosEntry() { RequestID = requestID, RequestStart = DateTime.UtcNow, IP = ip };

            request.RequestContext.HttpContext.Items["requestID"] = requestID;
        }

        public class DosEntry
        {
            public string IP;
            public DateTime RequestEnd = DateTime.MaxValue;
            public long RequestID;
            public DateTime RequestStart;
        }
    }
}