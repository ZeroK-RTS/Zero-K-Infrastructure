using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;
using PlanetWars.Utility;
using PlanetWarsShared;
using PlanetWarsShared.UnitSyncLib;
using Timer=System.Timers.Timer;

namespace PlanetWars.UI
{
	public partial class MainForm : Form
	{
		public static MainForm Instance;

        public MapBox MapBox { get; private set; }
		public ToolStripButton lastClicked;

		public MainForm()
		{
			if (Instance != null) {
				throw new Exception("mainform already exists");
			}

			Instance = this;
			InitializeComponent();

			new LoadingForm().ShowDialog();

			MapBox = new MapBox();
			MapBox.Show();

			MapBox.Dock = DockStyle.Fill;

			panel1.Controls.Add(MapBox);

			var timer = new Timer {Interval = Program.GalaxyRefreshSeconds*1000, AutoReset = true};
			timer.Elapsed += updateGalaxyTimer_Tick;
			timer.Start();
			base.Text = string.Format("{0} {1}", Application.ProductName, Application.ProductVersion);

			SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
		}

		void updateGalaxyTimer_Tick(object sender, EventArgs e)
		{
			var timer = sender as Timer;
			timer.Enabled = false;
			try {
				UpdateGalaxy();
			} catch (Exception ex) {
				MessageBox.Show(ex.ToString(), "Error updating galaxy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			} finally {
				timer.Enabled = true;
			}
		}

		public void UpdateGalaxy()
		{
			var lastChanged = Program.Server.LastChanged;
			if (lastChanged < Program.LastUpdate || Program.AuthInfo == null) {
				return;
			}
			List<Map> oldMaps = new List<Map>(GalaxyMap.Instance.Maps);
			var gal = Program.Server.GetGalaxyMap(Program.AuthInfo);
			Invoke(
				new Functor(
					() =>
					{
						GalaxyLoader.LoadGalaxy(false, null, gal);
						GalaxyLoader.LoadUpdateDoneFinalizeMaps(oldMaps);
					}));
		}

		void SetChecked(ToolStripButton bn)
		{
			btn_AddLink.Checked = false;
			btn_AddPlanet.Checked = false;
			btn_RemoveLink.Checked = false;
			btn_RemovePlanet.Checked = false;
			btn_SelectPlanet.Checked = false;
			bn.Checked = true;
		}

		void btn_AddPlanet_Click(object sender, EventArgs e)
		{
			SetChecked(btn_AddPlanet);
			lastClicked = btn_AddPlanet;
		}

		void btn_RemovePlanet_Click(object sender, EventArgs e)
		{
			SetChecked(btn_RemovePlanet);
			lastClicked = btn_RemovePlanet;
		}

		void btn_SelectPlanet_Click(object sender, EventArgs e)
		{
			SetChecked(btn_SelectPlanet);
			lastClicked = btn_SelectPlanet;
		}

		void btn_AddLink_Click(object sender, EventArgs e)
		{
			SetChecked(btn_AddLink);
			lastClicked = btn_AddLink;
		}

		void btn_DumpMinimaps_Click(object sender, EventArgs e)
		{
			const string exportPath = "C:\\Export";
			const string minimapPath = exportPath + "\\Minimaps";
			const string heightmapPath = exportPath + "\\Heightmaps";
			const string metalmapPath = exportPath + "\\Metalmaps";
			const string mapInfoPath = exportPath + "\\MapInfo";
			Directory.CreateDirectory(exportPath);
			Directory.CreateDirectory(minimapPath);
			Directory.CreateDirectory(heightmapPath);
			Directory.CreateDirectory(metalmapPath);
			Directory.CreateDirectory(mapInfoPath);
			using (var us = new UnitSync(Program.SpringPath)) {
				int i = 0;
				var count = GalaxyMap.Instance.Galaxy.MapNames.Count;
				foreach (var mapName in GalaxyMap.Instance.Galaxy.MapNames) {
					try {
						var map = new Map(mapName, us.GetMapInfo(mapName), us.GetMapChecksum(mapName));
						map.FixAspectRatio(us.GetMinimap(mapName, 0)).Save(minimapPath.Combine(mapName + ".png"), ImageFormat.Png);
						us.GetHeightMap(mapName).Save(heightmapPath.Combine(mapName + ".png"), ImageFormat.Png);
						us.GetMetalMap(mapName).Save(metalmapPath.Combine(mapName + ".png"), ImageFormat.Png);
						map.Save(mapInfoPath.Combine(mapName + ".dat"));
					} catch (Exception ex) {
						Debug.WriteLine(string.Format("{0} is corrupted: coult not export data. ({1}/{2}", mapName, i++, count));
					}
					Debug.WriteLine(
						"Extracted data from {0} ({1}/{2})".FormatWith(mapName, ++i, count));
				}
			}
		}

		void btn_SaveGalaxy_Click(object sender, EventArgs e)
		{
			lastClicked = btn_SaveGalaxy;
			using (var fs = new FileStream("galaxy.xml", FileMode.Create)) {
				new XmlSerializer(typeof (Galaxy)).Serialize(fs, GalaxyMap.Instance.Galaxy);
			}
		}

		void btn_SetAllowedMaps_Click(object sender, EventArgs e)
		{
			lastClicked = btn_SetAllowedMaps;
			var f = new Form();
			f.Controls.Add(new CustomPropertyGrid(new GalaxyProxy()));
			f.ShowDialog();
		}

		void btn_RemoveLink_Click(object sender, EventArgs e)
		{
			SetChecked(btn_RemoveLink);
			lastClicked = btn_RemoveLink;
		}

		void simulateButton_Click(object sender, EventArgs e)
		{
			Simulator.SimulateGame();
			UpdateAndRedraw();
		}

		void UpdateAndRedraw()
		{
			UpdateGalaxy();
			MapBox.Redraw();
		}

		void addPlayerButton_Click(object sender, EventArgs e)
		{
			Debug.WriteLine(Simulator.AddPlayer());
			UpdateAndRedraw();
		}

		void logButton_Click(object sender, EventArgs e)
		{
			GalaxyMap.Instance.Galaxy.Events.ForEach(ev => Debug.WriteLine(ev.ToHtml()));
		}

		void battleButton_Click(object sender, EventArgs e)
		{
			Debug.WriteLine(Simulator.MakeBattle());
			UpdateAndRedraw();
		}

		delegate void Functor();

		private void clearStateButton_Click(object sender, EventArgs e)
		{
			const string ServerStatePath = "serverstate.xml";
			var info = new FileInfo(ServerStatePath);
			if (info.Exists) {
				info.Delete();
			}
		}

		private void exportLinksButton_Click(object sender, EventArgs e)
		{
			var imagePath = "..\\WebSite\\galaxy\\galaxy.jpg";
			var mapSize = Image.FromFile(imagePath).Size;
		    var dumpPath = "..\\WebSite\\links";
			var path = Path.GetFullPath(dumpPath);
		    Directory.CreateDirectory(dumpPath);
			Directory.GetFiles(path).ForEach(File.Delete);
			var linkImageGenerator = new LinkImageGenerator(mapSize, GalaxyMap.Instance.Galaxy, path);
			linkImageGenerator.GenerateImages();
		}

		private void fitToScreenButton_Click(object sender, EventArgs e)
		{
			MapBox.ZoomFactor = 1;
		}

		private void exporXmlMapInfoButton_Click(object sender, EventArgs e)
		{
			const string exportPath = "C:\\Export";
			const string xmlMapInfoPath = exportPath + "\\XmlMapInfo";
			Directory.CreateDirectory(xmlMapInfoPath);
			using (var us = new UnitSync(Program.SpringPath)) {
				int i = 0;
				var count = GalaxyMap.Instance.Galaxy.MapNames.Count;
				foreach (var mapName in GalaxyMap.Instance.Galaxy.MapNames) {
                    var path = Path.Combine(xmlMapInfoPath, mapName + ".xml");
                    if (File.Exists(path)) {
                        continue;
                    }
					try {
						us.GetMapInfo(mapName).Save(path);
					} catch (Exception ex) {
                        
						Debug.WriteLine(string.Format("{0} is corrupted: coult not export data. ({1}/{2}) ({3})", mapName, i++, count, ex.Message));
					}
					Debug.WriteLine(
						"Extracted data from {0} ({1}/{2})".FormatWith(mapName, ++i, count));
				}
			}
		}
	}

	class GalaxyProxy
	{
		public string[] AllowedMaps
		{
			get { return GalaxyMap.Instance.Galaxy.MapNames.ToArray(); }
			set { GalaxyMap.Instance.Galaxy.MapNames = new List<string>(value); }
		}
	}
}