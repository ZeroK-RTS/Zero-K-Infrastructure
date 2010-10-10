using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using PlanetWars.Properties;
using PlanetWars.Utility;
using PlanetWarsShared;

namespace PlanetWars.UI
{
	public class MapBox : ScrollablePictureBox
	{
		const int SelectionCircleSize = 40;
		readonly Pen selectionPen = new Pen(Color.LightBlue, 5);
		Stopwatch clickwatch;
		Point cursorPosition;
		PlanetDrawing selectedPlanetDrawing;

		public MapBox()
		{
			base.BackColor = Color.Gray;
		    Image = Image.FromFile("../webSite/galaxy/galaxy.jpg");
			PictureBox.Paint += pictureBox_Paint;
			PictureBox.MouseDown += pictureBox_MouseDown;
			GalaxyLoader.GalaxyLoaded += (s, e) => Redraw();
		}

		public bool ShowText { get; set; }

		void DoClick(MouseEventArgs e)
		{
			var locationF = new PointF((float)e.Location.X/PictureBox.Width, (float)e.Location.Y/PictureBox.Height);
			var b = MainForm.Instance.lastClicked;
			if (b == null || b == MainForm.Instance.btn_SelectPlanet) {
				if (!GalaxyMap.Instance.PlanetDrawings.Any()) {
					return;
				}
				var planetDrawing = locationF.FindClosest(GalaxyMap.Instance.PlanetDrawings);
				selectedPlanetDrawing = planetDrawing;
				Redraw();
				if (planetDrawing.Map == null) {
					return;
				}
				var myPlanet = planetDrawing.Planet.OwnerName == Program.AuthInfo.Login;
				new PlanetInfoForm(
					planetDrawing, myPlanet && !GalaxyMap.Instance.Galaxy.GetPlayer(Program.AuthInfo.Login).HasChangedMap, myPlanet).
					Show();
				return;
			}
			if (b == MainForm.Instance.btn_AddPlanet) {
				AddPlanet(locationF);
			} else if (b == MainForm.Instance.btn_RemovePlanet) {
				RemovePlanet(locationF);
			} else if (b == MainForm.Instance.btn_AddLink) {
				AddLink(locationF);
			} else if (b == MainForm.Instance.btn_RemoveLink) {
				RemoveLink(locationF);
			}
			GalaxyMap.Instance.Galaxy.CheckIntegrity();
		}

		void RemoveLink(PointF locationF)
		{
			var g = GalaxyMap.Instance.Galaxy;
			if (g.Planets.Count < 2) {
				return;
			}
			if (selectedPlanetDrawing == null) {
				selectedPlanetDrawing = locationF.FindClosest(GalaxyMap.Instance.PlanetDrawings);
				Redraw();
			} else {
				var planetDrawing = locationF.FindClosest(GalaxyMap.Instance.PlanetDrawings);
				var link =
					GalaxyMap.Instance.Galaxy.Links.SingleOrDefault(
						l => g.GetPlanets(l).Contains(planetDrawing.Planet) && g.GetPlanets(l).Contains(selectedPlanetDrawing.Planet));
				if (link != null) {
					g.Links.Remove(link);
				}
				selectedPlanetDrawing = null;
				Redraw();
			}
		}

		void AddLink(PointF locationF)
		{
			if (GalaxyMap.Instance.PlanetDrawings.Count < 2) {
				return;
			}
			if (selectedPlanetDrawing == null) {
				selectedPlanetDrawing = locationF.FindClosest(GalaxyMap.Instance.PlanetDrawings);
				Redraw();
			} else {
				var planetDrawing = locationF.FindClosest(GalaxyMap.Instance.PlanetDrawings);
				if (
					!GalaxyMap.Instance.Galaxy.Links.Any(
					 	l =>
					 	GalaxyMap.Instance.Galaxy.GetPlanets(l).Contains(planetDrawing.Planet) &&
					 	GalaxyMap.Instance.Galaxy.GetPlanets(l).Contains(selectedPlanetDrawing.Planet))) {
					var link = new Link(selectedPlanetDrawing.Planet.ID, planetDrawing.Planet.ID);
					GalaxyMap.Instance.Galaxy.Links.Add(link);
					selectedPlanetDrawing = null;
					Redraw();
				}
			}
		}

		void RemovePlanet(PointF locationF)
		{
			if (!GalaxyMap.Instance.PlanetDrawings.Any()) {
				return;
			}
			var planetDrawing = locationF.FindClosest(GalaxyMap.Instance.PlanetDrawings);
			if (DialogResult.Yes ==
			    MessageBox.Show(
			    	"Do you want to delete planet " + planetDrawing.Planet.Name ?? "Unnamed" + "?",
			    	"Confirm Delete",
			    	MessageBoxButtons.YesNo)) {
				GalaxyMap.Instance.Galaxy.Planets.Remove(planetDrawing.Planet);
				GalaxyMap.Instance.PlanetDrawings.Remove(planetDrawing);
				GalaxyMap.Instance.Galaxy.Links.RemoveAll(l => l.PlanetIDs.Contains(planetDrawing.Planet.ID));
			    selectedPlanetDrawing = null;
			    Program.MainForm.Text = GalaxyMap.Instance.Galaxy.Planets.Count().ToString();
				Redraw();
			}
		}

		void AddPlanet(PointF locationF)
		{
			var planets = GalaxyMap.Instance.Galaxy.Planets;
			int ID = planets.Count > 0 ? planets.Select(p => p.ID).Max() + 1 : 1;
			var planet = new Planet {ID = ID};
			var names = Resources.stars.Replace("\r\n", "\n").Split('\n').Except(planets.Select(p => p.Name)).ToArray();
            planet.Name = names[Program.Random.Next(names.Length)];
			planet.Position = locationF;
			planets.Add(planet);
			GalaxyMap.Instance.PlanetDrawings.Add(new PlanetDrawing(planet));
            Program.MainForm.Text = GalaxyMap.Instance.Galaxy.Planets.Count().ToString();
			Redraw();
		}

		void pictureBox_MouseDown(object sender, MouseEventArgs e)
		{
			cursorPosition = e.Location;
			clickwatch = Stopwatch.StartNew();
			PictureBox.MouseMove += pictureBox_MouseMove;
			PictureBox.MouseUp += pictureBox_MouseUp;
		}

		void pictureBox_MouseUp(object sender, MouseEventArgs e)
		{
			PictureBox.MouseMove -= pictureBox_MouseMove;
			PictureBox.MouseUp -= pictureBox_MouseUp;
			if (clickwatch.ElapsedMilliseconds < 200) {
				DoClick(e);
			}
			clickwatch = null;
		}

		void pictureBox_MouseMove(object sender, MouseEventArgs e)
		{
			ScrollPosition = ScrollPosition.Translate(new Vector(e.Location, cursorPosition));
			cursorPosition = e.Location;
		}

		void pictureBox_Paint(object sender, PaintEventArgs e)
		{
			var galaxy = GalaxyMap.Instance.Galaxy;
			if (galaxy != null) {
				e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
				e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
				var mapSize = Image.Size.Scale(ZoomFactor);
				if (selectedPlanetDrawing != null) {
					e.Graphics.DrawEllipse(
						selectionPen,
						(int)(selectedPlanetDrawing.Position.X*mapSize.Width - SelectionCircleSize/2),
						(int)(selectedPlanetDrawing.Position.Y*mapSize.Height - SelectionCircleSize/2),
						SelectionCircleSize,
						SelectionCircleSize);
				}
				var linkImageMaker = new LinkImageGenerator(mapSize, galaxy, "links/");
				Directory.GetFiles(linkImageMaker.ImagePath).ForEach(File.Delete);

				foreach (var link in galaxy.Links) {
				    var points = link.PlanetIDs.Select(id => galaxy.GetPlanet(id).Position.Scale(mapSize).ToPoint()).ToArray();
                    using (var p = new Pen(Color.Green, 2)) {
                        e.Graphics.DrawLine(p, points[0], points[1]);
                    }
				}

				GalaxyMap.Instance.PlanetDrawings.ForEach(p => p.Draw(e.Graphics, mapSize));
				GalaxyMap.Instance.PlanetDrawings.ForEach(p => p.Label.Draw(e.Graphics, mapSize));
			}
		}

		public void DrawLink(Graphics g, Link link, Color color, Size mapSize)
		{
            using (var pen = new Pen(color, 2) { DashStyle = DashStyle.DashDot, LineJoin = LineJoin.Round, }) {
                var planets = GalaxyMap.Instance.Galaxy.GetPlanets(link).ToArray();
                g.DrawLine(pen, planets[0].Position.Scale(mapSize), planets[1].Position.Scale(mapSize));
            }
		}
	}
}