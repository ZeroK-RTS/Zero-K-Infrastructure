using System.Collections.Generic;

namespace PlasmaShared
{
    public class SpringBattleStartSetup
    {
        public List<ScriptKeyValuePair> ModOptions = new List<ScriptKeyValuePair>();
        public List<UserCustomParameters> UserParameters = new List<UserCustomParameters>();
        public BalanceTeamsResult BalanceTeamsResult;

        #region Nested type: ScriptKeyValuePair

        public class ScriptKeyValuePair
        {
            public string Key;
            public string Value;
        }

        #endregion

        #region Nested type: UserCustomParameters

        public class UserCustomParameters
        {
            public int LobbyID;
            public List<ScriptKeyValuePair> Parameters = new List<ScriptKeyValuePair>();
        }

        
        #endregion
    }
}