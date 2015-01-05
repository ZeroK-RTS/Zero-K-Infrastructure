using System.Linq;

namespace PlasmaShared
{
    public class AhConfig {
        public string Login;
        public string Password;
        public string[] JoinChannels;
        public string Title;
        public string Welcome;
        public string Map;
        public string Mod;
        public int MaxPlayers;
        public int? SplitBiggerThan;
        public bool AutoSpawnClones;
        public string AutoUpdateRapidTag;
        public string SpringVersion;
        public string AutoUpdateSpringBranch;
        public string BattlePassword;
        public AutohostMode Mode;
        public CommandLevel[] CommandLevels;
        public int? MaxEloDifference;
        public int? MinToJuggle;
        public int? MaxToJuggle;
        public AhConfig() {}
    }
}