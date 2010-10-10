using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using PlanetWars.UI;
using PlanetWars.Utility;
using PlanetWarsShared;

namespace PlanetWars
{
	class GalaxyLoader
	{
		readonly BackgroundWorker backGroundWorker = new BackgroundWorker();
		readonly bool getAll;

		public GalaxyLoader(bool getAll)
		{
			this.getAll = getAll;
			backGroundWorker.WorkerReportsProgress = true;
			backGroundWorker.DoWork += backGroundWorker_DoWork;
		}

		public static event EventHandler GalaxyLoaded = delegate { };

		public void RunWorkerAsync()
		{
			backGroundWorker.RunWorkerAsync();
		}

		public event ProgressChangedEventHandler ProgressChanged
		{
			add { backGroundWorker.ProgressChanged += value; }
			remove { backGroundWorker.ProgressChanged -= value; }
		}

		public event RunWorkerCompletedEventHandler RunWorkerCompleted
		{
			add { backGroundWorker.RunWorkerCompleted += value; }
			remove { backGroundWorker.RunWorkerCompleted -= value; }
		}

		public static void LoadUpdateDoneFinalizeMaps(List<Map> oldMaps)
		{
			var g = GalaxyMap.Instance;
			var usedMaps = g.Galaxy.Planets.Select(p => p.MapName).Where(n => n != null);
			g.Maps = oldMaps;
			if (oldMaps.Any(m => m == null)) {
				throw new Exception("null map 1");
			}
			foreach (var mapName in usedMaps) {
				if (!g.Maps.Any(m => m.Name == mapName)) {
					var map = Map.FromDisk(mapName);
					if (map != null) {
						GalaxyMap.Instance.Maps.Add(map);
					}
				}
			}
			g.Maps.ForEach(m => g.PlanetDrawings.Where(p => p.Planet.MapName == m.Name).ForEach(x => x.Map = m));
			if (g.Maps.Any(m => m == null)) {
				throw new Exception("null map 3");
			}
		}

		void backGroundWorker_DoWork(object sender, DoWorkEventArgs ea)
		{
			try {
				LoadGalaxy(getAll, backGroundWorker, null);
			} catch {
				Program.LastUpdate = DateTime.MinValue;
				throw;
			} finally {
				GalaxyLoaded(this, EventArgs.Empty);
			}
		}

		/// <summary>
		/// Loads galaxy from server
		/// </summary>
		/// <param name="all">should get all maps</param>
		/// <param name="worker">should report to background worker</param>
		/// <param name="loadedGalaxy">use this galaxy state</param>
		public static void LoadGalaxy(bool all, BackgroundWorker worker, Galaxy loadedGalaxy)
		{
			if (all && worker == null) {
				throw new ApplicationException("All needs worker");
			}
			Directory.CreateDirectory(Program.CachePath);
			Directory.CreateDirectory(Program.MapInfoCache);
			Directory.CreateDirectory(Program.MetalmapCache);
			Directory.CreateDirectory(Program.HeightmapCache);
			Directory.CreateDirectory(Program.MinimapCache);
			GalaxyMap.Instance = null;
			if (worker != null) {
				worker.ReportProgress(0, new[] {"Connecting"});
			}
			bool done = Program.AuthInfo != null;
			do {
				if (!done) {
					var loginForm = new LoginForm();
					if (DialogResult.OK != loginForm.ShowDialog()) {
						Application.Exit();
					}
					Program.AuthInfo = loginForm.AuthInfo;
				}
				Program.LastUpdate = DateTime.Now;

				GalaxyMap.Instance.Galaxy = loadedGalaxy ?? Program.Server.GetGalaxyMap(Program.AuthInfo);
				// either use loadedgalaxy or get new one
				done = GalaxyMap.Instance.Galaxy != null;
				if (!done) {
					MessageBox.Show("Invalid username or password.");
				}
			} while (!done);
			var galaxyMap = GalaxyMap.Instance;
			var galaxy = galaxyMap.Galaxy;
			galaxy.Planets.ForEach(p => galaxyMap.PlanetDrawings.Add(new PlanetDrawing(p)));

			var q = (from f in galaxy.Factions
			         from p in galaxy.GetAttackOptions(f)
			         select new {p.ID, f});
			galaxyMap.AttackablePlanetIDs = q.ToDictionary(t => t.ID, t => t.f);

			galaxyMap.ClaimablePlanetIDs = galaxy.GetClaimablePlanets().Select(p => p.ID).ToArray();	

			var neededMaps = all
			                 	? galaxy.MapNames.Except(galaxyMap.Maps.Select(m => m.Name))
			                 	: galaxy.Planets.Select(p => p.MapName).Where(n => n != null);
			var mapsToDownload = neededMaps.Where(n => !File.Exists(Program.MapInfoCache.Combine(n + ".dat"))).ToArray();

			int maxProgress = mapsToDownload.Length*4;
			WebClient Client = new WebClient();
			int progress = 0;

			if (worker != null) {
				Action<string, string> MapReport =
					(message, mapName) =>
					worker.ReportProgress(progress++*100/maxProgress, new[] {message + Map.GetHumanName(mapName), mapName});
#if false 
				foreach (var mapName in mapsToDownload) {
					MapReport("Downloading Map Info", mapName);
					Client.DownloadFile(Program.MapInfoUrl + "/" + mapName + ".dat", Program.MapInfoCache.Combine(mapName + ".dat"));


					MapReport("Downloading Minimap", mapName);
					Client.DownloadFile(Program.MinimapUrl + "/" + mapName + ".jpg", Program.MinimapCache.Combine(mapName + ".jpg"));
					MapReport("Downloading Heightmap", mapName);
					Client.DownloadFile(
						Program.HeightmapUrl + "/" + mapName + ".jpg", Program.HeightmapCache.Combine(mapName + ".jpg"));
					MapReport("Downloading Metalmap", mapName);
					Client.DownloadFile(Program.MetalmapUrl + "/" + mapName + ".png", Program.MetalmapCache.Combine(mapName + ".png"));
					worker.ReportProgress(progress*100/maxProgress, mapName);

				}
#endif
			}
		}
	}
}