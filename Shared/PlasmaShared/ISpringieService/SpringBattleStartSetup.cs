using System.Collections.Generic;
using System.Linq;

namespace PlasmaShared
{
    public class SpringBattleStartSetup
    {
        public BattleContext StartContext;

        public Dictionary<string, string> ModOptions = new Dictionary<string, string>();
        public Dictionary<string, Dictionary<string, string>> UserParameters = new Dictionary<string, Dictionary<string, string>>();


        public SpringBattleStartSetup(BattleContext startContext)
        {
            StartContext = startContext;
        }
    }
}