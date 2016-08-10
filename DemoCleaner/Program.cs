using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DemoCleaner
{
    class Program
    {
        static void Main(string[] args)
        {
            Trace.Listeners.Add(new ConsoleTraceListener(false) {TraceOutputOptions = TraceOptions.None});
            var dc = new DemoCleaner();
            dc.CleanAllFiles();
        }
    }
}
