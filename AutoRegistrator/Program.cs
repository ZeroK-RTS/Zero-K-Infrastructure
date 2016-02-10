using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroKWeb;

namespace Autoregistrator
{
    class Program
    {
        static void Main(string[] args) {
            Trace.Listeners.Add(new ConsoleTraceListener());
            var ar = new AutoRegistrator();
            ar.Main();

        }
    }
}
