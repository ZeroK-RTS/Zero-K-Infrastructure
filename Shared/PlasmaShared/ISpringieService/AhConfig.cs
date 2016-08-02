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
        public bool AutoSpawnClones;
        public string AutoUpdateRapidTag;
        public string SpringVersion;
        public string BattlePassword;
        public AutohostMode Mode;
        public CommandLevel[] CommandLevels;
        public int? MaxEloDifference;
        public AhConfig() {}
    }
}