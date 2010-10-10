using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using PlanetWars.Utility;

namespace PlanetWars.UI
{
	class ChangeMapControl : Control
	{
		readonly ToolStripButton button = new ToolStripButton("Download Map Information");
		readonly ChangeMapBox changeMapBox = new ChangeMapBox();
		readonly GalaxyLoader loader = new GalaxyLoader(true);

		readonly ProgressBar progressBar = new ProgressBar
		{Style = ProgressBarStyle.Blocks, Dock = DockStyle.Bottom, Visible = false};

		readonly ToolStrip toolStrip = new ToolStrip
		{Stretch = true, Dock = DockStyle.Bottom, GripStyle = ToolStripGripStyle.Hidden};

		public ChangeMapControl()
		{
			base.Dock = DockStyle.Fill;
			toolStrip.Items.AddRange(new ToolStripItem[] {button});
			Controls.AddRange(new Control[] { progressBar, toolStrip, changeMapBox });
			button.Click += button_Click;
			ReloadMaps();
			changeMapBox.BringToFront();
		}

		void ReloadMaps()
		{
			changeMapBox.ReloadMaps();
			toolStrip.Visible =
				GalaxyMap.Instance.Galaxy.MapNames.Any(n => !File.Exists(Program.MapInfoCache.Combine(n + ".dat")));
		}

		void button_Click(object sender, EventArgs e)
		{
			toolStrip.Visible = false;
			progressBar.Visible = true;
			loader.ProgressChanged += loader_ProgressChanged;
			loader.RunWorkerCompleted += loader_RunWorkerCompleted;
			loader.RunWorkerAsync();
		}

		void loader_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			var mapName = e.UserState as string;
			var mapInfo = e.UserState as string[];
			progressBar.Value = e.ProgressPercentage;
			if (mapInfo != null) {
				changeMapBox.ToolTip.SetToolTip(changeMapBox, mapName);
			}
			if (mapName != null) {
				var map = Map.FromDisk(mapName);
				GalaxyMap.Instance.Maps.Add(map);
				var planetDrawing = GalaxyMap.Instance.PlanetDrawings.SingleOrDefault(p => p.Planet.MapName == mapName);
				if (planetDrawing != null) {
					planetDrawing.Map = map;
				}
				Invalidate();
				Update();
			}
		}

		void loader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			progressBar.Visible = false;
			Invalidate();
			Update();
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if (progressBar.Visible) {
				changeMapBox.ToolTip.ToolTipTitle = null;
				Cursor = Cursors.Default;
			}
		}
	}
}