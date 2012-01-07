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
        public AutohostMode GetMode() {
            if (AutohostName.StartsWith("PlanetWars")) return AutohostMode.Planetwars;
            else return AutohostMode.GameTeams;        
        }
    }

    public class PlayerTeam
    {
        public int LobbyID;
        public int AllyID;
        public string Name;
        public bool IsSpectator;
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