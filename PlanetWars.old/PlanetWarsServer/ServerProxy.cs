#region using

using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Contexts;
using PlanetWarsShared;
using PlanetWarsShared.Springie;



#endregion

namespace PlanetWarsServer
{
	[Synchronization]
	public class ServerProxy : ContextBoundObject, ISpringieServer, IServer
	{
		Server s
		{
			get { return Server.Instance; }
		}

		#region IServer Members

		public DateTime LastChanged
		{
			get { return s.LastChanged; }
		}

	    public Galaxy GetGalaxyMap(AuthInfo authorization)
		{
			return s.GetGalaxyMap(authorization);
		}

		public bool ChangePlanetMap(string mapName, AuthInfo authorization, out string message)
		{
			return s.ChangePlanetMap(mapName, authorization, out message);
		}

		public bool ChangePlanetName(string newName, AuthInfo authorization, out string message)
		{
			return s.ChangePlanetName(newName, authorization, out message);
		}

	    public bool ChangePlanetDescription(string newDescription, AuthInfo authorization, out string message)
	    {
	        return s.ChangePlanetDescription(newDescription, authorization, out message);
	    }

	    public bool ChangeCommanderInChiefTitle(string newTitle, AuthInfo authorization, out string message)
		{
			return s.ChangeCommanderInChiefTitle(newTitle, authorization, out message);
		}

	    public bool SendAid(string toPlayer, double ammount, AuthInfo authorization, out string message)
	    {
	        return s.SendAid(toPlayer, ammount, authorization, out message);
	    }

	    public bool ChangePlayerPassword(string newPassword, AuthInfo authorization, out string message)
		{
			return s.ChangePlayerPassword(newPassword, authorization, out message);
		}

		#endregion

		#region ISpringieServer Members

		public ICollection<IPlanet> GetAttackOptions(AuthInfo springieLogin)
		{
			return s.GetAttackOptions(springieLogin);
		}

        public ICollection<IFaction> GetFactions(AuthInfo springieLogin)
		{
            return s.GetFactions(springieLogin);
		}

		public ICollection<string> GetPlayersToNotify(AuthInfo springieLogin, string mapName, ReminderEvent reminderEvent)
		{
			return s.GetPlayersToNotify(springieLogin, mapName, reminderEvent);
		}

        public IFaction GetOffensiveFaction(AuthInfo springieLogin)
		{
            return s.GetOffensiveFaction(springieLogin);
		}

		public IPlayer GetPlayerInfo(AuthInfo springieLogin, string name)
		{
            return s.GetPlayerInfo(springieLogin, name);
		}

		public string Register(AuthInfo springieLogin, AuthInfo account, string side, string planet)
		{
			return s.Register(springieLogin, account, side, planet);
		}

		public string GetStartupModOptions(AuthInfo springieLogin, string mapName, ICollection<IPlayer> players)
		{
			return s.GetStartupModOptions(springieLogin, mapName, players);
		}

		public SendBattleResultOutput SendBattleResult(AuthInfo springieLogin, string mapName, ICollection<EndGamePlayerInfo> participants)
		{
			return s.SendBattleResult(springieLogin, mapName, participants);
		}

		#endregion

		#region IServer Members


		public bool SetReminderOptions(ReminderEvent reminderEvent, ReminderLevel reminderLevel, ReminderRoundInitiative reminderRoundInitiative, AuthInfo authorization, out string message)
		{
			return s.SetReminderOptions(reminderEvent, reminderLevel, reminderRoundInitiative, authorization, out message);
		}
        public bool SetTimeZone(TimeZoneInfo LocalTimeZone, AuthInfo authorization, out string message)
        {
            return s.SetTimeZone(LocalTimeZone, authorization, out message);
        }

	    public void BuyUpgrade(AuthInfo login, int upgradeDefID, string choice)
	    {
	        s.BuyUpgrade(login, upgradeDefID, choice);
	    }

	    #endregion

		#region IServer Members



		public ICollection<UpgradeDef> GetAvailableUpgrades(Galaxy galaxy, string playerName)
		{
			return s.GetAvailableUpgrades(galaxy, playerName);
		}

		public IDictionary<string, List<UpgradeDef>> UpgradeData
		{
			get { return s.UpgradeData;}
		}


	    public void ForceSaveState()
	    {
	        s.ForceSaveState();
	    }

	    public IDictionary<string, SpringieState> GetSpringieStates()
	    {
	        return s.GetSpringieStates();
	    }

	    public bool SendBlockadeFleet(AuthInfo login, int targetPlanetID, out string message)
	    {
	        return s.SendBlockadeFleet(login, targetPlanetID, out message);
	    }

	    public void AddAward(AuthInfo springieLogin, string playerName, string awardType, string awardText, string mapName)
	    {
	        s.AddAward(springieLogin, playerName, awardType, awardText, mapName);
	    }

	    public void SendChatLine(AuthInfo springieLogin, string channel, string playerName, string text)
	    {
	        s.SendChatLine(springieLogin, channel, playerName, text);
	    }

	    #endregion

		#region ISpringieServer Members

		public void UnitDeployed(AuthInfo springieLogin, string mapName, string playerName, string unit, int x, int z, string rotation)
		{
			s.UnitDeployed(springieLogin, mapName, playerName, unit, x, z, rotation);
		}


        public string ResetPassword(AuthInfo springieLogin, string loginName)
        {
           return s.ResetPassword(springieLogin, loginName);
        }

	    public ICollection<string> GetFactionChannelAllowedExceptions()
	    {
	        return s.GetFactionChannelAllowedExceptions();
	    }

	    public void UnitDied(AuthInfo springieLogin, string playerName, string unitName, int x, int z)
	    {
	        s.UnitDied(springieLogin, playerName, unitName, x, z);
	    }

	    public void UnitPurchased(AuthInfo springieLogin, string playerName, string unitName, double cost, int x, int z)
	    {
	        s.UnitPurchased(springieLogin, playerName, unitName, cost, x, z);
	    }

	    #endregion
    }
}