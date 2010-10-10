using System;
using PlanetWarsShared;
using PlanetWarsShared.Springie;
using System.Collections.Generic;


namespace PlanetWarsShared
{
	public interface IServer
	{
		DateTime LastChanged { get; }
		Galaxy GetGalaxyMap(AuthInfo authorization);

		/// <summary>
		/// Lets a player change his planet's map.
		/// </summary>
		/// <param name="mapName">Name of the new map. Must be an unused map.</param>
		/// <param name="authorization">Login info of the planet's owner.</param>
		/// <param name="message">Message describing the operation's results.</param>
		/// <returns>Message describing the operation's results.</returns>
		bool ChangePlanetMap(string mapName, AuthInfo authorization, out string message);


		/// <summary>
		/// Lets a player change his planet's name.
		/// </summary>
		/// <param name="newName">New name of the planet. Must be an unused name. Must be between 3 and 20 chars.</param>
		/// <param name="authorization">Login info of the planet's owner.</param>
		/// <param name="message">Message describing the operation's results.</param>
		/// <returns>Boolean describing whether the operation was successful.</returns>
		bool ChangePlanetName(string newName, AuthInfo authorization, out string message);

	    bool ChangePlanetDescription(string newDescription, AuthInfo authorization, out string message);


		/// <summary>
		/// Lets a Commander-in-chief change his title.
		/// </summary>
		/// <param name="newTitle">New title. Must be between 3 and 20 chars.</param>
		/// <param name="authorization">Login info of the Commander-in-chief.</param>
		/// <param name="message">Message describing the operation's results.</param>
		/// <returns>Boolean describing whether the operation was successful.</returns>
		bool ChangeCommanderInChiefTitle(string newTitle, AuthInfo authorization, out string message);

	    bool SendAid(string toPlayer, double ammount, AuthInfo authorization, out string message);

		bool ChangePlayerPassword(string newPassword, AuthInfo authorization, out string message);

		bool SetReminderOptions(ReminderEvent reminderEvent, ReminderLevel reminderLevel,
		                        ReminderRoundInitiative reminderRoundInitiative, AuthInfo authorization, out string message);

	    bool SetTimeZone(TimeZoneInfo LocalTimeZone,
                            AuthInfo authorization,
                          out string message);
        
		void BuyUpgrade(AuthInfo login, int upgradeDefID, string choice);
		ICollection<UpgradeDef> GetAvailableUpgrades(Galaxy galaxy, string playerName);
		IDictionary<string, List<UpgradeDef>> UpgradeData { get; }
	    void ForceSaveState();
	    IDictionary<string, SpringieState> GetSpringieStates();
	    bool SendBlockadeFleet(AuthInfo login, int targetPlanetID, out string message);

	}
}