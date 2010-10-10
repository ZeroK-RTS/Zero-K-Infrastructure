using System.Collections.Generic;
using System.Drawing;
using PlanetWars.Properties;
using PlanetWarsShared;

namespace PlanetWars
{
	class GalaxyMap
	{
		static GalaxyMap instance;

		GalaxyMap()
		{
			instance = this;
			Background =  Image.FromFile(@"c:\work\planetwars\PlanetWars\Images\galaxy.jpg"); //Resources.galaxy;
			PlanetDrawings = new List<PlanetDrawing>();
			Maps = new List<Map>();
		}

		public static GalaxyMap Instance
		{
			get { return instance ?? new GalaxyMap(); }
			set { instance = value; }
		}

		public Galaxy Galaxy { get; set; }
		public Image Background { get; set; }
		public List<PlanetDrawing> PlanetDrawings { get; set; }
		public List<Map> Maps { get; set; }
		public Dictionary<int, Faction> AttackablePlanetIDs { get; set; }
		public int[] ClaimablePlanetIDs { get; set; }
	}
}