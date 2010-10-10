using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Threading;
using PlanetWarsShared;
using System.Linq;
using PlanetWarsShared.Events;

namespace PlanetWarsServer
{
	class Program
	{
		static Mutex mutex;

		#region Other methods


        static Dictionary<string, double> playerElo = new Dictionary<string, double>();

		static void Main(string[] args)
		{
            string fileNameAlphaNumeric = "";
            foreach (var curChar in Process.GetCurrentProcess().MainModule.FileName.ToString())
                if (Char.IsLetterOrDigit(curChar)) fileNameAlphaNumeric += curChar;

            mutex = new Mutex(false, "PlanetWarsServer" + fileNameAlphaNumeric);
			if (!mutex.WaitOne(15000, false)) {
				throw new Exception("More than one server process running.");
			}
		    
            Server s = Server.Instance;
		    s.Galaxy.RecalculateAllEloRanks(); 

			RemotingConfiguration.Configure(string.Format("{0}.config", Process.GetCurrentProcess().MainModule.FileName), false);
			RemotingConfiguration.RegisterWellKnownServiceType(typeof (ServerProxy), "IServer", WellKnownObjectMode.Singleton);
			do {
				Thread.Sleep(10000);
			} while (true);
            
		}

		#endregion
	}
}