#region using

using System;
using System.Collections.Generic;
using System.Drawing;
using PlanetWarsShared.Springie;
using System.Diagnostics;

#endregion

namespace PlanetWarsShared
{
	[Serializable]
	[DebuggerDisplay("{Name} ({ID}, {FactionName}, {OwnerName}, {MapName})")]
	public class Planet : IPlanet
	{
		#region Properties

		public bool IsStartingPlanet;
		PointF position;
		public string FactionName { get; set; }

		public PointF Position
		{
			get { return position; }
			set
			{
				if (value.X > 1 || value.Y > 1) {
					throw new ArgumentOutOfRangeException();
				}
				position = value;
			}
		}

		#endregion

		#region Constructors

		public Planet(int id, float x, float y)
		{
			ID = id;
			Position = new PointF(x, y);
		}

		public Planet(int id, float x, float y, string ownerName, string factionName) : this(id, x, y)
		{
			OwnerName = ownerName;
			FactionName = factionName;
		}

		public Planet() {}

		#endregion

		#region IPlanet Members

		public string OwnerName { get; set; }

		public int ID { get; set; }

		public string MapName { get; set; }
		public string Name { get; set; }

		public List<Rectangle> StartBoxes { get; set; }

		public List<string> AutohostCommands
		{
			get { return new List<string> {"!preset continues"}; }
		}

		#endregion

		public override string ToString()
		{
			return ToHtml(Name,ID);
		}

		public static string ToHtml(string name, int id)
		{
			return string.Format("<a href='planet.aspx?name={0}&id={1}'>{0}</a>", name, id);
		}
	}
}