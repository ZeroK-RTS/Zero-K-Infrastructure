using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using ZkData;

namespace ZkLobbyServer
{
    public class DelayLogger
    {

        static long maxDelay = 0;
        static long minDelay = int.MaxValue;
        static long sumDelay = 0;
        static long reports = 0;
        static ConcurrentDictionary<string, int> counts = new ConcurrentDictionary<string, int>();
        static DateTime lastReport = DateTime.UtcNow;


        static object mathlock = new object();


        public static void ReportDelay(long milliseconds, string type)
        {
            counts.AddOrUpdate(type, 1, (a, i) => i + 1);
            lock (mathlock)
            {
                maxDelay = Math.Max(milliseconds, maxDelay);
                minDelay = Math.Min(milliseconds, minDelay);
                sumDelay += milliseconds;
                reports++;
                if (DateTime.UtcNow.Subtract(lastReport).TotalSeconds >= GlobalConst.ProcessTimeLoggingIntervalSeconds)
                {
                    Trace.TraceInformation("Lobby command processing time for last " + reports + " delayed commands. Avg: " + (sumDelay / reports) + " ms, Min: " + (minDelay) + " ms, Max: " + (maxDelay) + " ms.");
                    foreach (var pair in counts)
                    {
                        Trace.TraceInformation(pair.Key + " lagged " + pair.Value + " times.");
                    }
                    reports = 0;
                    maxDelay = 0;
                    minDelay = int.MaxValue;
                    sumDelay = 0;
                    lastReport = DateTime.UtcNow;
                    counts = new ConcurrentDictionary<string, int>();
                }
            }

        }
    }
}