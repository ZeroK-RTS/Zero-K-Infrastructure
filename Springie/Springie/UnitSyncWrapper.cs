#region using

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Timers;
using System.Xml.Serialization;
using PlasmaShared;
using PlasmaShared.UnitSyncLib;

#endregion

namespace Springie.SpringNamespace
{
	public class UnitSyncWrapper: IDisposable
	{
		const string ServerResourceUrlBase = "http://planet-wars.eu/PlasmaServer/Resources";
		readonly object locker = new object();

		readonly Dictionary<string, Map> mapList = new Dictionary<string, Map>();
		readonly Dictionary<string, Mod> modList = new Dictionary<string, Mod>();
		readonly SpringPaths path;
		readonly Timer timer;

		public Dictionary<string, Map> MapList { get { return mapList; } }

		public Dictionary<string, Mod> ModList { get { return modList; } }

		public event EventHandler NotifyModsChanged = delegate { };

		public UnitSyncWrapper(SpringPaths path)
		{
			this.path = path;
			var bf = new BinaryFormatter();

			try
			{
				using (var fs = File.OpenRead(Path.Combine(path.Cache, "mapinfo.dat"))) mapList = (Dictionary<string, Map>)bf.Deserialize(fs);
			}
			catch (Exception ex)
			{
				ErrorHandling.HandleException(ex, "While loading mapinfo.dat");
			}
			var loadedModList = new Dictionary<string, Mod>();
			try
			{
				using (var fs = File.OpenRead(Path.Combine(path.Cache, "modinfo.dat"))) loadedModList = (Dictionary<string, Mod>)bf.Deserialize(fs);
			}
			catch (Exception ex)
			{
				ErrorHandling.HandleException(ex, "While loading modinfo.dat");
			}
			modList = new Dictionary<string, Mod>(loadedModList);

			if (mapList.Count == 0) mapList["dummy"] = new Map("dummy");
			if (modList.Count == 0) modList["dummy"] = new Mod() { Name = "dummy" };

			foreach (var mod in modList.Values) FixModOptions(mod);

			timer = new Timer(10*60*1000);
			timer.Elapsed += (s, e) =>
				{
					var plasmaService = new PlasmaService();
					plasmaService.GetResourceListCompleted += plasmaService_GetResourceListCompleted;
					plasmaService.GetResourceListAsync();
				};
			timer.AutoReset = true;
			timer.Start();

			var ser = new PlasmaService();
			ser.GetResourceListCompleted += plasmaService_GetResourceListCompleted;
			ser.GetResourceListAsync();
		}

		public void Dispose()
		{
			timer.Stop();
		}

		public string GetFirstMap()
		{
			IEnumerator<Map> enu = MapList.Values.GetEnumerator();
			enu.MoveNext();
			return enu.Current.Name;
		}

		public string GetFirstMod()
		{
			IEnumerator<Mod> enu = ModList.Values.GetEnumerator();
			enu.MoveNext();
			return enu.Current.Name;
		}

		public Map GetMapInfo(string name)
		{
			lock (locker)
			{
				var mi = mapList[name];
				if (mi.Positions == null && mi.Gravity == 0)
				{
					var url = "";
					try
					{
						url = String.Format("{0}/{1}.metadata.xml.gz", ServerResourceUrlBase, mi.Name.EscapePath());
						var data = new WebClient().DownloadData(url);
						if (data != null)
						{
							mi = (Map)new XmlSerializer(typeof(Map)).Deserialize(new MemoryStream(data.Decompress()));
							if (mi != null)
							{
								mi.Name = mi.Name.Replace(".smf", ""); // hack remove this after server data reset
								mapList[mi.Name] = mi;
								SaveMapInfo();
							}
						}
					}
					catch (Exception ex)
					{
						ErrorHandling.HandleException(ex, string.Format("Error getting map info from url: {0}", url));
					}
				}
				return mi;
			}
		}

		public Mod GetModInfo(string name)
		{
			lock (locker)
			{
				var mi = modList[name];
				if (mi.Sides == null)
				{
					var url = "";
					try
					{
						url = String.Format("{0}/{1}.metadata.xml.gz", ServerResourceUrlBase, mi.Name.EscapePath());
						var data = new WebClient().DownloadData(url);
						if (data != null)
						{
							mi = (Mod)new XmlSerializer(typeof(Mod)).Deserialize(new MemoryStream(data.Decompress()));
							if (mi != null)
							{
								FixModOptions(mi);
								modList[mi.Name] = mi;
								SaveModInfo();
							}
						}
					}
					catch (Exception ex)
					{
						ErrorHandling.HandleException(ex, string.Format("Error getting mod info from: {0}", url));
					}
				}
				return mi;
			}
		}


		public bool HasMap(string name)
		{
			return mapList.ContainsKey(name);
		}


		public bool HasMod(string modName)
		{
			lock (locker)
			{
				if (!modList.ContainsKey(modName))
				{
					foreach (var p in modList) if (p.Value.ArchiveName == modName) return true;
					return false;
				}
				else return true;
			}
		}

		void FixModOptions(Mod mod)
		{
			if (mod != null && mod.Options != null) foreach (var option in mod.Options) if (option.Type == OptionType.Number) option.Default = option.Default.Replace(",", ".");
		}

		void SaveMapInfo()
		{
			lock (locker)
			{
				var bf = new BinaryFormatter();
				try
				{
					using (var fs = File.OpenWrite(Path.Combine(path.Cache, "mapinfo.dat"))) bf.Serialize(fs, mapList);
				}
				catch (Exception ex)
				{
					ErrorHandling.HandleException(ex, "While loading mapinfo.dat");
				}
			}
		}

		void SaveModInfo()
		{
			lock (locker)
			{
				var bf = new BinaryFormatter();
				try
				{
					using (var fs = File.OpenWrite(Path.Combine(path.Cache, "modinfo.dat"))) bf.Serialize(fs, modList);
				}
				catch (Exception ex)
				{
					ErrorHandling.HandleException(ex, "While loading modinfo.dat");
				}
			}
		}

		void plasmaService_GetResourceListCompleted(object sender, GetResourceListCompletedEventArgs e)
		{
			lock (locker)
			{
				var ser = sender as PlasmaService;
				if (ser != null) ser.GetResourceListCompleted -= plasmaService_GetResourceListCompleted;
				var changed = false;
				foreach (var x in e.Result)
				{
					SpringHashEntry curHash = null;
					var springVer = Program.main.Config.SpringVersion;
					if (string.IsNullOrEmpty(springVer)) springVer = path.SpringVersion;
					if (!string.IsNullOrEmpty(springVer)) curHash = x.SpringHashes.SingleOrDefault(y => y.SpringVersion == springVer);
					else curHash = x.SpringHashes.Last();

					if (curHash != null)
					{
						var hash = curHash.SpringHash;
						if (x.ResourceType == ResourceType.Mod)
						{
							Mod mod;
							if (modList.TryGetValue(x.InternalName, out mod))
							{
								if (mod.Checksum != hash) changed = true;
								mod.Checksum = hash;
							}
							else
							{
								changed = true;
								modList[x.InternalName] = new Mod() { Checksum = hash, Name = x.InternalName };
							}
						}
						else if (x.ResourceType == ResourceType.Map)
						{
							Map map;
							if (mapList.TryGetValue(x.InternalName, out map))
							{
								if (map.Checksum != hash) changed = true;
								map.Checksum = hash;
							}
							else
							{
								changed = true;
								mapList[x.InternalName] = new Map(x.InternalName) { Checksum = hash };
							}
						}
					}
				}
				if (changed)
				{
					SaveMapInfo();
					SaveModInfo();
				}
				if (changed && NotifyModsChanged != null) NotifyModsChanged(this, EventArgs.Empty);
			}
		}
	}
}