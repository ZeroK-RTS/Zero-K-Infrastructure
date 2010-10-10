using System;
using System.Xml.Serialization;
using System.IO;

namespace PlanetWarsShared
{
	[Serializable]
	public class Link
	{
		[XmlIgnore]
		int[] planetIDs;

		public Link() {}

		public Link(int ID1, int ID2)
		{
			PlanetIDs = new[] {ID1, ID2};
		}

		public int[] PlanetIDs
		{
			get { return planetIDs; }
			set
			{
				if (value.Length != 2) {
					throw new Exception("A link can have only two planets");
				}
				planetIDs = value;
			}
		}

		[XmlIgnore]
		public int this[int index]
		{
			get { return planetIDs[index]; }
			set { planetIDs[index] = value; }
		}

		public string GetFileName(Galaxy galaxy)
		{
			var faction1 = galaxy.GetPlanet(PlanetIDs[0]).FactionName;
			var faction2 = galaxy.GetPlanet(PlanetIDs[1]).FactionName;
			var offensiveFaction = galaxy.OffensiveFaction.Name;
			return GetFileName(galaxy, faction1, faction2, offensiveFaction);
		}

		public string GetFileName(Galaxy galaxy, string faction1, string faction2, string offensiveFaction)
		{
			return String.Format("{0}_{1}_{2}_{3}_{4}.png",
				PlanetIDs[0],
				PlanetIDs[1],
				faction1??"neutral",
				faction2??"neutral",
				faction1 != faction2 && faction1 != null && faction2 != null 
					? offensiveFaction
					: String.Empty);
		}

		public override string ToString()
		{
			return PlanetIDs[0] + " " + PlanetIDs[1];
		}
	}
}