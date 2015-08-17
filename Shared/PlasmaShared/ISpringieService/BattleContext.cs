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
        public string Title;
        public bool IsMission;
        public string EngineVersion;
        public Dictionary<int, BattleRect> Rectangles { get; set; } = new Dictionary<int, BattleRect>();
        public IDictionary<string, string> ModOptions { get; set; } = new Dictionary<string, string>();
    }
}