using System.Collections.Generic;
using System.Drawing;

namespace PlanetWarsShared.Springie
{
	public interface IPlanet
	{
		string MapName { get; }
		string Name { get; }
		List<Rectangle> StartBoxes { get; }
		List<string> AutohostCommands { get; }
		string OwnerName { get; }
		int ID { get; set; }
	}
}