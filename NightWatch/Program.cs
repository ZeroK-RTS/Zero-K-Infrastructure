using System;
using System.Diagnostics;
using System.Threading;

namespace CaTracker
{
    public static class Program
    {
        public static Main MainInstance;

        public static void Main(string[] args)
        {
        		Trace.Listeners.Add(new ConsoleTraceListener());
            MainInstance = new Main();
            Trace.TraceInformation("Starting Tracker");
            MainInstance.Start();
						
            while (true) Thread.Sleep(10000);
        }
    }
}