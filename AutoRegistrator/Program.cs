using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoRegistrator;
using ZeroKWeb;
using ZkData;

namespace Autoregistrator
{
    class Program
    {
        static void Main(string[] args) {
            Trace.Listeners.Add(new ConsoleTraceListener());
            //var ar = new ZeroKWeb.AutoRegistrator();
            //ar.Main(@"c:\temp\testf");

            var spg = new SteamDepotGenerator();
            spg.Generate(@"c:\work\Zero-K-Infrastructure\Zero-K.info",@"c:\temp\spg", ModeType.Live);
        }
    }
}
