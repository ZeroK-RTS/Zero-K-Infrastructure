namespace LobbyClient
{
    public class BotBattleStatus: UserBattleStatus
    {
        public string aiLib;
        public string owner;

        public BotBattleStatus(string name, string owner, string aiLib): base(name, null)
        {
            this.owner = owner;
            this.aiLib = aiLib;
        }

    } ;
}