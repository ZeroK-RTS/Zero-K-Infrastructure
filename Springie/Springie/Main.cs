using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using System.Xml.Serialization;
using LobbyClient;
using PlasmaShared;
using Springie.autohost;
using Springie.SpringNamespace;

namespace Springie
{
	/// <summary>
	/// Holds and handles autohost instances
	/// </summary>
	/// 
	public class Main
	{
		public const string ConfigMain = "main.xml";

		readonly List<AutoHost> autoHosts = new List<AutoHost>();
		List<AutoHost> deletionCandidate = new List<AutoHost>();
		readonly SpringPaths paths;
		readonly Timer timer;

		public string RootWorkPath { get; private set; }
		public SpringieServer SpringieServer = new SpringieServer();
		public UnitSyncWrapper UnitSyncWrapper { get; private set; }
		public MainConfig Config;
		public PlasmaDownloader.PlasmaDownloader Downloader;

		public Main(string path)
		{
			RootWorkPath = path;
			LoadConfig();
			SaveConfig();
      paths = new SpringPaths(Path.GetDirectoryName(Config.ExecutableName));
			paths.DedicatedServer = Config.ExecutableName;
			paths.Cache = path;
			paths.MakeFolders();

			UnitSyncWrapper = new UnitSyncWrapper(paths);

			timer = new Timer(30000);
			timer.Elapsed += timer_Elapsed;
			timer.AutoReset = true;
			timer.Start();

			Downloader = new PlasmaDownloader.PlasmaDownloader(Config, null, paths);
		}

		public int GetFreeHostingPort()
		{
			lock (autoHosts)
			{
				var usedPorts = autoHosts.ToDictionary(x => x.hostingPort);
				var freePort = Enumerable.Range(Config.HostingPortStart, Config.MaxInstances).FirstOrDefault(x => !usedPorts.ContainsKey(x));
				return freePort;
			}
		}


		public void LoadConfig()
		{
			Config = new MainConfig();
			if (File.Exists(RootWorkPath + '/' + ConfigMain))
			{
				var s = new XmlSerializer(Config.GetType());
				var r = File.OpenText(RootWorkPath + '/' + ConfigMain);
				Config = (MainConfig)s.Deserialize(r);
				r.Close();
			}
		}


		public void SaveConfig()
		{
			var s = new XmlSerializer(Config.GetType());
			var f = File.OpenWrite(RootWorkPath + '/' + ConfigMain);
			f.SetLength(0);
			s.Serialize(f, Config);
			f.Close();
		}

		public void SpawnAutoHost(string path, SpawnConfig spawnData)
		{
			lock (autoHosts)
			{
				var ah = new AutoHost(paths, UnitSyncWrapper, path, GetFreeHostingPort(), spawnData);
				autoHosts.Add(ah);
			}
		}


		public void StartAll()
		{
			StopAll();

			var foldersWithConfig = Directory.GetDirectories(RootWorkPath).Where(x => File.Exists(Path.Combine(x, AutoHost.ConfigName)));

			if (foldersWithConfig.Count() == 0) // no subfolders, start default instance
				StartFromPath(RootWorkPath);
			else foreach (var folder in foldersWithConfig) StartFromPath(folder);
		}

		public void StartFromPath(string path)
		{
			SpawnAutoHost(path, null);
		}


		public void StopAll()
		{
			lock (autoHosts)
			{
				foreach (var ah in autoHosts) ah.Dispose();
				autoHosts.Clear();
			}
		}

		public void StopAutohost(AutoHost ah)
		{
			ah.Dispose();
			lock (autoHosts)
			{
				autoHosts.Remove(ah);
			}
		}

		void timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			try
			{
				lock (autoHosts)
				{
					// spawned autohosts
					var spawnedToDel = autoHosts.Where(x => x.SpawnConfig != null && (x.tas.MyBattle == null || x.tas.MyBattle.Users.Count <= 1)).ToList();
					foreach (var ah in spawnedToDel.Where(x => deletionCandidate.Contains(x))) StopAutohost(ah); // delete those who are empty during 2 checks
					deletionCandidate = spawnedToDel;

					// autohosts which have clones
					var keys = autoHosts.Where(x => x.config.AutoSpawnClone).Select(x => x.config.AccountName).Distinct().ToList();
					foreach (var key in keys)
					{
						// 0-1 players = empty
						var empty =
							autoHosts.Where(
								x =>
								x.SpawnConfig == null && x.config.AccountName == key &&
								(x.tas.MyBattle == null || (x.tas.MyBattle.Users.Count <= 1 && !x.tas.MyUser.IsInGame))).ToList();

						if (empty.Count == 1) continue;

						else if (empty.Count == 0)
						{
							var existing = autoHosts.Where(x => x.config.AccountName == key).First();
							StartFromPath(existing.configPath);
						}
						else // more than 1 empty running, stop all but 1
						{
							var minNumber = empty.Min(y => y.CloneNumber);
							foreach (var ah in empty.Where(x => x.CloneNumber != minNumber && x.SpawnConfig == null)) StopAutohost(ah);
						}
					}
				}
			}
			catch (Exception ex)
			{
				ErrorHandling.HandleException(ex, "While checking autohosts");
			}
		}
	}

	public class SpawnConfig
	{
		public string Mod;
		public string Owner;
		public string Password;
		public string Title;

		public SpawnConfig(string owner, Dictionary<string, string> config)
		{
			Owner = owner;
			config.TryGetValue("password", out Password);
			config.TryGetValue("mod", out Mod);
			config.TryGetValue("title", out Title);
		}
	}
}