using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
            var sitePath = GlobalConst.SiteDiskPath;
            if (args.Length > 0) sitePath = args[0];

            var ar = new ZeroKWeb.AutoRegistrator(sitePath);
            ar.Main();

            var spg = new SteamDepotGenerator(sitePath, Path.Combine(sitePath,"..","steamworks","tools","ContentBuilder","content"));
            spg.Generate();
            spg.RunBuild();
        }
    }
}
