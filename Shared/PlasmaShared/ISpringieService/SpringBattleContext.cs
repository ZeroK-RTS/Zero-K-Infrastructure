using System;
using System.Collections.Generic;
using System.Linq;

namespace PlasmaShared
{
    public class SpringBattleContext
    {
        public BattleContext StartContext;
        public Dictionary<string, string> ModOptions = new Dictionary<string, string>();
        public Dictionary<string, Dictionary<string, string>> UserParameters = new Dictionary<string, Dictionary<string, string>>();

        public int Duration;
        public string EngineBattleID;
        public DateTime? IngameStartTime;
        public DateTime StartTime;
        public string ReplayName;

        public List<string> OutputExtras = new List<string>();



        public SpringBattleContext(BattleContext startContext)
        {
            StartContext = startContext;
        }
    }
}