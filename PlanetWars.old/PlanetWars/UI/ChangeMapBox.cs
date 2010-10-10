using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using PlanetWars.Properties;
using PlanetWars.Utility;

namespace PlanetWars.UI
{
	class ChangeMapBox : ScrollablePictureBox
	{
		const int MapSize = 96;

		int columns;
		int rows;

		public ChangeMapBox()
		{
			ToolTip = new ToolTip();
			base.Dock = DockStyle.Fill;
			base.BackColor = Color.Black;
			ToolTip.AutoPopDelay = 0;
			SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
		}

		public ToolTip ToolTip { get; private set; }
		public string[] AvailableMaps { get; private set; }

		public void ReloadMaps()
		{
			var g = GalaxyMap.Instance;
			AvailableMaps = g.Galaxy.GetAvailableMaps().ToArray();
			foreach (var mapName in g.Galaxy.MapNames) {
				if (!g.Maps.Any(p => p.Name == mapName)) {
					var map = Map.FromDisk(mapName);
					if (map != null) {
						g.Maps.Add(map);
					}
				}
			}
			PictureBox.Image = GenerateBitmap();
		}

		Bitmap GenerateBitmap()
		{
			int width = 400;
			columns = Math.Max((width - 10) / MapSize, 1);
			rows = AvailableMaps.Length / columns + 1;
			var bitmap = new Bitmap(columns * MapSize, columns * MapSize);
			AutoScrollMinSize = bitmap.Size;
			Size = bitmap.Size;
			using (var g = Graphics.FromImage(bitmap)) {
				g.InterpolationMode = InterpolationMode.HighQualityBicubic;
				int mapIndex = 0;
				for (int row = 0; row < rows; row++) {
					for (int column = 0; column < columns; column++) {
						if (AvailableMaps.Length <= mapIndex) {
							goto Outside;
						}
						var map = GalaxyMap.Instance.Maps.FirstOrDefault(m => m.Name == AvailableMaps[mapIndex]);
						var image = map == null ? Resources.questionmark : map.Minimap;
						g.DrawImage(image, column*MapSize, row*MapSize, MapSize, MapSize);
						mapIndex++;
					}
				}
			}
			Outside:
			return bitmap;
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			Invalidate();
		}

		string GetMapName(Point location)
		{
			int row = location.X/MapSize;
			int column = location.Y/MapSize;
			if (column > columns - 1) {
				return null;
			}
			int mapIndex = column*columns + row;
			if (mapIndex < AvailableMaps.Length) {
				return AvailableMaps[mapIndex];
			}
			return null;
		}

		protected override void OnMouseClick(MouseEventArgs e)
		{
			base.OnMouseClick(e);
			var mapName = GetMapName(e.Location);
			if (mapName != null) {
				var map = GalaxyMap.Instance.Maps.FirstOrDefault(m => m.Name == mapName);
				if (map != null && DialogResult.OK == new MinimapForm(map).ShowDialog()) {
					string outcome;
					;
					if (!Program.Server.ChangePlanetMap(map.Name, Program.AuthInfo, out outcome)) {
						MessageBox.Show(outcome ?? "An error has occurred.");
					}
					new LoadingForm().Show();
					((Form)Parent.Parent).Close();
				}
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			var mapName = GetMapName(e.Location);
			if (mapName == null) {
				ToolTip.Hide(this);
				Cursor = Cursors.Default;
			} else {
				var map = GalaxyMap.Instance.Maps.FirstOrDefault(m => m.Name == mapName);
				if (map != null) {
					Cursor = Cursors.Hand;
					ToolTip.ToolTipTitle = Map.GetHumanName(mapName);
					var text =
						"Description: {0}\nSize: {1}x{2}\nGravity: {3}\nWind: {4}-{5}\nMetal: {6}\nTidal Strength: {7}\n".FormatWith(
							map.Description,
							map.Size.Width,
							map.Size.Height,
							map.Gravity,
							map.MinWind,
							map.MaxWind,
							map.MaxMetal,
							map.TidalStrength);
					if (!map.Author.IsNullOrEmpty()) {
						text = "Author:" + map.Author + "\n" + text;
					}
					ToolTip.SetToolTip(this, text.Replace("\n", Environment.NewLine));
				} else {
					ToolTip.ToolTipTitle = null;
					ToolTip.SetToolTip(this, Map.GetHumanName(mapName));
					Cursor = Cursors.Default;
				}
			}
		}
	}
}