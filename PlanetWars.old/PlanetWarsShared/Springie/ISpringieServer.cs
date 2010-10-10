using System;
using System.Collections.Generic;

namespace PlanetWarsShared.Springie
{
    [Serializable]
    public class RankNotification
    {
        public string Name;
        public string Text;
        public RankNotification() {}
        public RankNotification(string name, string text)
        {
            Name = name;
            Text = text;
        }
    }

    [Serializable]
    public class SendBattleResultOutput
    {
        public string MessageToDisplay = "";
        public List<RankNotification> RankNotifications = new List<RankNotification>();
        public SendBattleResultOutput() {}
    }

	public interface ISpringieServer
	{
		ICollection<IPlanet> GetAttackOptions(AuthInfo springieLogin);
		ICollection<IFaction> GetFactions(AuthInfo springieLogin);
		ICollection<string> GetPlayersToNotify(AuthInfo springieLogin, string mapName, ReminderEvent reminderEvent); 
		IFaction GetOffensiveFaction(AuthInfo springieLogin);
		IPlayer GetPlayerInfo(AuthInfo springieLogin, string name);
		string Register(AuthInfo springieLogin, AuthInfo account, string side, string planet);
		string GetStartupModOptions(AuthInfo springieLogin, string mapName, ICollection<IPlayer> players);
		SendBattleResultOutput SendBattleResult(AuthInfo springieLogin, string mapName, ICollection<EndGamePlayerInfo> participants);
		void UnitDeployed(AuthInfo springieLogin, string mapName, string playerName, string unit, int x, int z, string rotation);
        void AddAward(AuthInfo springieLogin, string playerName, string awardType, string awardText, string mapName);
	    void SendChatLine(AuthInfo springieLogin, string channel, string playerName, string text);
        string ResetPassword(AuthInfo springieLogin, string loginName);
	    ICollection<string> GetFactionChannelAllowedExceptions();
	    void UnitDied(AuthInfo springieLogin, string playerName, string unitName, int x, int z);
	    void UnitPurchased(AuthInfo springieLogin, string playerName, string unitName, double cost, int x, int z);
	}
}