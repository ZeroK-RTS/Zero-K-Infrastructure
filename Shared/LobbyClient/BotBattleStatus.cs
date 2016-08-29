using System;
using PlasmaShared;

namespace LobbyClient
{
    public class BotBattleStatus:UserBattleStatus
    {
        public string aiLib;
        public string owner;

        public BotBattleStatus(string name, string owner, string aiLib):base(name,null)
        {
            this.owner = owner;
            this.aiLib = aiLib;
        }

        public UpdateBotStatus ToUpdateBotStatus()
        {
            return new UpdateBotStatus()
            {
                Name = Name,
                AllyNumber = AllyNumber,
                Owner = owner,
                AiLib = aiLib
            };
        }
        
        public void UpdateWith(UpdateBotStatus u)
        {
            if (u != null)
            {
                if (u.Name != Name) throw new Exception(string.Format("Applying update of {0} to user {1}", u.Name, Name));
                if (u.AllyNumber.HasValue) AllyNumber = u.AllyNumber.Value;
                if (u.AiLib != null) aiLib = u.AiLib;
            }
        }

        public BotTeam ToBotTeam()
        {
            return new BotTeam() { BotName = this.Name, AllyID = this.AllyNumber, Owner = this.owner, BotAI = this.aiLib };
        }
    } ;
}