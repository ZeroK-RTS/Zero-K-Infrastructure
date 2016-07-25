using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DemoCleaner
{
    class Program
    {
        static void Main(string[] args)
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
            var dc = new DemoCleaner();
            dc.CleanAllFiles();
        }
    }
}
