using System.Collections.Generic;

namespace PlasmaShared
{
    public class BalanceTeamsResult
    {
        public List<BotTeam> Bots = new List<BotTeam>();
        public bool CanStart = true;
        public bool DeleteBots;
        public string Message;
        public List<PlayerTeam> Players = new List<PlayerTeam>();
    }
}