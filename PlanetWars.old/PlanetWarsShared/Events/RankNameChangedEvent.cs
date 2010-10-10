using System;

namespace PlanetWarsShared.Events
{
	[Serializable]
	public class RankNameChangedEvent : Event
	{
		public RankNameChangedEvent(DateTime dateTime, string playerName, string oldRank, string newRank, Galaxy galaxy)
			: base(dateTime, galaxy)
		{
			PlayerName = playerName;
			OldRank = oldRank;
			NewRank = newRank;
		}

		RankNameChangedEvent() {}

		public string PlayerName { get; set; }
		public string OldRank { get; set; }
		public string NewRank { get; set; }

		public override bool IsPlayerRelated(string playerName)
		{
			return playerName == PlayerName;
		}

		public override bool IsPlanetRelated(int planetID)
		{
			return false;
		}

		public override bool IsFactionRelated(string factionName)
		{
			return Galaxy.GetPlayer(PlayerName).FactionName == factionName;
		}

		public override string ToHtml()
		{
			return string.Format("{0} will be henceforth known as {1} {0}.", Player.ToHtml(PlayerName), NewRank);
		}
	}
}