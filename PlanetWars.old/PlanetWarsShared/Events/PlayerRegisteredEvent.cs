using System;

namespace PlanetWarsShared.Events
{
	[Serializable]
	public class PlayerRegisteredEvent : Event
	{
		public PlayerRegisteredEvent(DateTime dateTime, string playerName, int? planetID, Galaxy galaxy)
			: base(dateTime, galaxy)
		{
			PlayerName = playerName;
			PlanetID = planetID;
		}

		PlayerRegisteredEvent() {}
		public string PlayerName { get; set; }
		public int? PlanetID { get; set; }

		public override bool IsPlayerRelated(string playerName)
		{
			return playerName == PlayerName;
		}

		public override bool IsPlanetRelated(int planetID)
		{
			return PlanetID == planetID;
		}

		public override bool IsFactionRelated(string factionName)
		{
			return Galaxy.GetPlayer(PlayerName).FactionName == factionName;
		}

		public override string ToHtml()
		{
			var factionName = Galaxy.GetPlayer(PlayerName).FactionName;
			return PlanetID == null
			       	? string.Format("{0} has joined {1}.", Player.ToHtml(PlayerName), Faction.ToHtml(factionName))
			       	: string.Format(
			       	  	"{0} has claimed {1} for {2}.", Player.ToHtml(PlayerName), Galaxy.GetPlanet(PlanetID.Value), Faction.ToHtml(factionName));
		}
	}

    
}