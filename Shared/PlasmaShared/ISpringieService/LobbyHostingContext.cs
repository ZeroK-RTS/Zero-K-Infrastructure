using System.Collections.Generic;
using ZkData;

namespace PlasmaShared
{
    public class LobbyHostingContext
    {
        public string FounderName;
        public string Map;
        public string Mod;
        public List<PlayerTeam> Players = new List<PlayerTeam>();
        public List<BotTeam> Bots = new List<BotTeam>();
        public string Title;
        public bool IsMission;
        public string EngineVersion;
        public bool IsMatchMakerGame;
        public RatingCategory ApplicableRating;
        public AutohostMode Mode = AutohostMode.None;
        public Dictionary<string, string> ModOptions = new Dictionary<string, string>();
        public Dictionary<string, string> MapOptions = new Dictionary<string, string>();
        public Dictionary<string,Dictionary<string,string>> UserParameters = new Dictionary<string, Dictionary<string, string>>();
        public int BattleID { get; set; }

        public void ApplyBalance(BalanceTeamsResult balance)
        {
            if (balance.DeleteBots) Bots.Clear();
            if (balance.Bots?.Count > 0) Bots = balance.Bots;
            if (balance.Players?.Count > 0) Players = balance.Players;
        }
    }
}