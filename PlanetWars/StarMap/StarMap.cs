using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PlanetWars.MapIcons;

namespace PlanetWars
{
	public class StarMap
	{
		public IEnumerable<CelestialObject> CelestialObjects;
		public IEnumerable<CelestialObjectLink> ObjectLinks;
		public IEnumerable<Player> Players;

		public double GetGameTurn(double value)
		{
			var gameTurn = 0.0;

			/*if (Config.Started.HasValue)
			{
				var seconds = Config.LocalGameSecond - Config.Started.Value.Second;
				gameTurn = seconds / Config.SecondsPerTurn;
				gameTurn += offsetSeconds / Config.SecondsPerTurn;
			}*/
			return gameTurn;

		}
	}

	public class CelestialObjectLink
	{
        private int FirstObjectIDField;
      
        private int SecondObjectIDField;


	}
}
