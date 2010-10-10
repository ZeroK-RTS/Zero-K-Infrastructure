using System;

namespace PlanetWarsShared.Events
{
	[Serializable]
	public class PlanetNameChangedEvent : Event
	{
		public PlanetNameChangedEvent(DateTime dateTime, int planetID, string oldName, string newName, string ownerName, Galaxy galaxy)
			: base(dateTime, galaxy)
		{
			PlanetID = planetID;
			OldName = oldName;
			NewName = newName;
		    OwnerName = ownerName;
		}

		PlanetNameChangedEvent() {}

		public int PlanetID { get; set; }
		public string OldName { get; set; }
		public string NewName { get; set; }
        public string OwnerName { get; set; }

		public override bool IsFactionRelated(string factionName)
		{
			return Galaxy.GetOwner(PlanetID).FactionName == factionName;
		}

		public override string ToHtml()
		{
			return string.Format("{0} has renamed his planet from {1} to {2}.", Player.ToHtml(OwnerName ?? Galaxy.GetOwner(PlanetID).Name),OldName, NewName);
		}

		public override bool IsPlayerRelated(string playerName)
		{
			return Galaxy.GetOwner(PlanetID).Name == playerName;
		}

		public override bool IsPlanetRelated(int planetID)
		{
			return planetID == PlanetID;
		}
	}
}