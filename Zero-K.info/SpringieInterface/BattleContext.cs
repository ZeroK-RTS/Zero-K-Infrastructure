using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ZkData;

namespace ZeroKWeb.SpringieInterface
{

    public class BattleContext
    {
        public string AutohostName;
        public string Map;
        public string Mod;
        public List<PlayerTeam> Players = new List<PlayerTeam>();
        public List<BotTeam> Bots = new List<BotTeam>();
        private AutohostMode? mode;
        
        public AutohostMode GetMode() {
            if (mode != null) return mode.Value;

            var db = new ZkDataContext();
            var name = AutohostName.TrimEnd('0','1','2','3','4','5','6','7','8','9');
            var entry = db.AutohostConfigs.SingleOrDefault(x => x.Login == name);
            if (entry != null) mode = entry.AutohostMode;
            else
            {
                if (AutohostName.StartsWith("PlanetWars")) mode =AutohostMode.Planetwars;
                else mode = AutohostMode.None;
            }
            return mode.Value;
        }
    }

    public class PlayerTeam
    {
        public int LobbyID;
        public int AllyID;
        public string Name;
        public bool IsSpectator;
        public bool IsIngame;
        public int TeamID;
    }

    public class BotTeam
    {
        public int AllyID;
        public string BotAI;
        public string BotName;
        public string Owner;
        public int TeamID;
    }


}