using System.Collections.Generic;
using ZkData;

namespace PlasmaShared
{
    public class BattleContext
    {
        public string AutohostName;
        public string Map;
        public string Mod;
        public List<PlayerTeam> Players = new List<PlayerTeam>();
        public List<BotTeam> Bots = new List<BotTeam>();
        private AutohostConfig config;
        public bool CanPlanetwars = false;

        public AutohostConfig GetConfig() {
            if (config != null) return config;
            if (string.IsNullOrEmpty(AutohostName)) return null;
            var db = new ZkDataContext();
            var name = AutohostName.TrimNumbers();
            var entry = db.AutohostConfigs.SingleOrDefault(x => x.Login == name);
            if (entry != null) config = entry;
            return config;
        }

        public AutohostMode GetMode() {
            if (GetConfig() != null) return GetConfig().AutohostMode;else return AutohostMode.None;
        }
    }
}