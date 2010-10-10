using System;
using System.IO;

namespace PlanetWarsShared.Events
{
	[Serializable]
	public class MapChangedEvent : Event
	{
		MapChangedEvent() {}

		public MapChangedEvent(DateTime dateTime, string oldMap, string newMap, int planetID, Galaxy galaxy)
			: base(dateTime, galaxy)
		{
			PlanetID = planetID;
			OldMap = oldMap;
			NewMap = newMap;
		}

		public int PlanetID { get; set; }
		public string OldMap { get; set; }
		public string NewMap { get; set; }

		public override bool IsPlanetRelated(int planetID)
		{
			return planetID == PlanetID;
		}

		public override bool IsPlayerRelated(string playerName)
		{
			return Galaxy.GetOwner(PlanetID).Name == playerName;
		}

		public override bool IsFactionRelated(string factionName)
		{
			return Galaxy.GetOwner(PlanetID).FactionName == factionName;
		}

		public override string ToHtml()
		{
			return String.Format("Planet {0} was terraformed from {1} to {2}", Galaxy.GetPlanet(PlanetID), Path.GetFileNameWithoutExtension(OldMap), Path.GetFileNameWithoutExtension(NewMap));
		}
	}
}