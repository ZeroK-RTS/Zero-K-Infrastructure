#region using

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using PlanetWarsShared;

#endregion

namespace PlanetWars
{
	public class PlanetDrawing : IDrawable, IPositionable
	{
		#region Properties

		const int PlanetSize = 5;
	    const int OutlineSize = 2;
		public MapLabel Label { get; set; }
		public Map Map { get; set; }
		public Planet Planet { get; set; }

		#endregion

		#region Constructors

		public PlanetDrawing(Planet planet)
		{
			Planet = planet;
			Label = new MapLabel(planet.Position);
			if (GalaxyMap.Instance.Galaxy.GetOwner(planet) == null) {
				Label.Text = Planet.Name;
			} else {
				Label.Text = Planet.Name + Environment.NewLine + GalaxyMap.Instance.Galaxy.GetOwner(planet).Name +
				             Environment.NewLine + Map.GetHumanName(planet.MapName);
			}
		}

		#endregion

		#region Overrides

		public override string ToString()
		{
			return Planet.Name;
		}

		#endregion

		#region IDrawable Members

		public void Draw(Graphics g, Size mapSize)
		{
			bool drawSquares = false;
			var galaxyMap = GalaxyMap.Instance;
			var galaxy = galaxyMap.Galaxy;
			var faction = galaxy.GetFaction(Planet);
			var color = faction != null ? faction.Color : Color.White;
			using (var brush = new SolidBrush(color)) {
				var x1 = (int)(Position.X*mapSize.Width - PlanetSize/2);
				var y1 = (int)(Position.Y*mapSize.Height - PlanetSize/2);

             
                g.FillEllipse(Brushes.Black, x1 - OutlineSize, y1 - OutlineSize, PlanetSize + OutlineSize * 2, PlanetSize + OutlineSize * 2);
				g.FillEllipse(brush, x1, y1, PlanetSize, PlanetSize);

                

				if (drawSquares) {
					g.SmoothingMode = SmoothingMode.None;
					g.InterpolationMode = InterpolationMode.NearestNeighbor;

					Faction attackFaction;
					var found = galaxyMap.AttackablePlanetIDs.TryGetValue(Planet.ID, out attackFaction);
					if (found) {
						g.DrawRectangle(new Pen(attackFaction.Color, 3), x1, y1, PlanetSize, PlanetSize);
					}

					if (galaxyMap.ClaimablePlanetIDs.Contains(Planet.ID)) {
						g.DrawRectangle(new Pen(Color.Purple, 3), x1, y1, PlanetSize, PlanetSize);
					}
					g.SmoothingMode = SmoothingMode.AntiAlias;
					g.InterpolationMode = InterpolationMode.HighQualityBicubic;
				}
			}
		}

		#endregion

		#region IPositionable Members

		public PointF Position
		{
			get { return Planet.Position; }
		}

		#endregion
	}
}