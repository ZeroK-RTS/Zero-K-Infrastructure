using System.Collections.Generic;
using System.Linq;

namespace PlasmaShared
{
    public class SpringBattleStartSetup
    {
        public BattleContext StartContext;
        public List<ScriptKeyValuePair> ModOptions = new List<ScriptKeyValuePair>();
        public List<UserCustomParameters> UserParameters = new List<UserCustomParameters>();


        public SpringBattleStartSetup(BattleContext startContext)
        {
            StartContext = startContext;
        }

        public class ScriptKeyValuePair
        {
            public string Key;
            public string Value;
        }

        public class UserCustomParameters
        {
            public int LobbyID;
            public List<ScriptKeyValuePair> Parameters = new List<ScriptKeyValuePair>();
        }
    }
}