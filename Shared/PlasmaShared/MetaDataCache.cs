#region using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Xml.Serialization;
using PlasmaShared.UnitSyncLib;

#endregion

namespace PlasmaShared
{
	public class MetaDataCache
	{
		const string ServerResourceUrlBase = "http://zero-k.info/Resources";

		public delegate void MapCallback(Map map, byte[] minimap, byte[] heightmap, byte[] metalmap);

		readonly Dictionary<string, List<MapRequestCallBacks>> currentMapRequests = new Dictionary<string, List<MapRequestCallBacks>>();
		readonly Dictionary<string, List<ModRequestCallBacks>> currentModRequests = new Dictionary<string, List<ModRequestCallBacks>>();
		// these metadata requests are ongoing

		readonly object mapRequestsLock = new object();
		readonly object modRequestsLock = new object();

		// ok its ugly hardcoded, but service stub itself has it hardcoed :)

		readonly string resourceFolder;
		readonly SpringScanner scanner;
		readonly WebClient webClientForMap = new WebClient() { Proxy = null };
		readonly WebClient webClientForMod = new WebClient() { Proxy = null };

		public MetaDataCache(SpringPaths springPaths, SpringScanner scanner)
		{
			this.scanner = scanner;
			resourceFolder = Utils.MakePath(springPaths.Cache, "Resources");
			Utils.CheckPath(resourceFolder);
		}

		public string GetHeightmapPath(string name)
		{
			return string.Format("{0}/{1}.heightmap.jpg", resourceFolder, name.EscapePath());
		}

		public void GetMapAsync(string mapName, MapCallback callback, Action<Exception> errorCallback)
		{
			Utils.StartAsync(() => GetMap(mapName, callback, errorCallback));
		}

		public Map GetMapMetadata(string name)
		{
			var data = File.ReadAllBytes(GetMetadataPath(name));
			return GetMapMetadata(data);
		}

		public Map GetMapMetadata(byte[] data)
		{
			var ret = (Map)new XmlSerializer(typeof(Map)).Deserialize(new MemoryStream(data.Decompress()));
			ret.Name = ret.Name.Replace(".smf", ""); // hack remove this after server data reset
			var hash = scanner.GetSpringHash(ret.Name);
			if (hash != 0) ret.Checksum = hash;
			return ret;
		}

		public string GetMetadataPath(string name)
		{
			return string.Format("{0}/{1}.metadata.xml.gz", resourceFolder, name.EscapePath());
		}

		public string GetMetalmapPath(string name)
		{
			return string.Format("{0}/{1}.metalmap.jpg", resourceFolder, name.EscapePath());
		}

		public string GetMinimapPath(string name)
		{
			return string.Format("{0}/{1}.minimap.jpg", resourceFolder, name.EscapePath());
		}

		public void GetModAsync(string modName, Action<Mod> callback, Action<Exception> errorCallback)
		{
			Utils.StartAsync(() => GetMod(modName, callback, errorCallback));
		}

		public Mod GetModMetadata(byte[] data)
		{
			var ret = (Mod)new XmlSerializer(typeof(Mod)).Deserialize(new MemoryStream(data.Decompress()));
			var hash = scanner.GetSpringHash(ret.Name);
			if (hash != 0) ret.Checksum = hash;
			if (ret.Options != null) foreach (var option in ret.Options) if (option.Type == OptionType.Number) option.Default = option.Default.Replace(",", ".");
			return ret;
		}

		public Mod GetModMetadata(string name)
		{
			var data = File.ReadAllBytes(GetMetadataPath(name));
			return GetModMetadata(data);
		}

		public bool HasEntry(string name)
		{
			return File.Exists(GetMetadataPath(name));
		}


		public void SaveHeightmap(string name, byte[] data)
		{
			File.WriteAllBytes(GetHeightmapPath(name), data);
		}

		/// <summary>
		/// call this as last
		/// </summary>
		/// <param name="name"></param>
		/// <param name="data"></param>
		public void SaveMetadata(string name, byte[] data)
		{
			File.WriteAllBytes(GetMetadataPath(name), data);
		}

		public void SaveMetalmap(string name, byte[] data)
		{
			File.WriteAllBytes(GetMetalmapPath(name), data);
		}

		public void SaveMinimap(string name, byte[] data)
		{
			File.WriteAllBytes(GetMinimapPath(name), data);
		}

		public static byte[] SerializeAndCompressMetaData(IResourceInfo info)
		{
			var serializedStream = new MemoryStream();
			new XmlSerializer(info.GetType()).Serialize(serializedStream, info);
			serializedStream.Position = 0;
			return serializedStream.ToArray().Compress();
		}


		void GetMap(string mapName, MapCallback callback, Action<Exception> errorCallback)
		{
			var metadataPath = GetMetadataPath(mapName);
			var minimapFile = GetMinimapPath(mapName);
			var metalMapFile = GetMetalmapPath(mapName);
			var heightMapFile = GetHeightmapPath(mapName);

			if (File.Exists(metadataPath))
			{
				// map found
				Map map = null;
				try
				{
					map = GetMapMetadata(mapName);
				}
				catch (Exception e)
				{
					Trace.WriteLine("Unable to deserialize map " + mapName + " from disk: " + e);
				}
				if (map != null)
				{
					var minimapBytes = File.Exists(minimapFile) ? File.ReadAllBytes(minimapFile) : null;
					var heightMapBytes = File.Exists(heightMapFile) ? File.ReadAllBytes(heightMapFile) : null;
					var metalMapBytes = File.Exists(metalMapFile) ? File.ReadAllBytes(metalMapFile) : null;
					callback(map, minimapBytes, heightMapBytes, metalMapBytes);
					return;
				}
			}

			try
			{
				lock (mapRequestsLock)
				{
					List<MapRequestCallBacks> list;
					if (currentMapRequests.TryGetValue(mapName, out list))
					{
						list.Add(new MapRequestCallBacks(callback, errorCallback));
						return;
					}
					else currentMapRequests[mapName] = new List<MapRequestCallBacks>() { new MapRequestCallBacks(callback, errorCallback) };
				}

				byte[] minimap = null;
				byte[] metalmap = null;
				byte[] heightmap = null;
				byte[] metadata = null;

				lock (webClientForMap)
				{
					minimap = webClientForMap.DownloadData(String.Format("{0}/{1}.minimap.jpg", ServerResourceUrlBase, mapName.EscapePath()));

					metalmap = webClientForMap.DownloadData(String.Format("{0}/{1}.metalmap.jpg", ServerResourceUrlBase, mapName.EscapePath()));

					heightmap = webClientForMap.DownloadData(String.Format("{0}/{1}.heightmap.jpg", ServerResourceUrlBase, mapName.EscapePath()));

					metadata = webClientForMap.DownloadData(String.Format("{0}/{1}.metadata.xml.gz", ServerResourceUrlBase, mapName.EscapePath()));
				}

				var map = GetMapMetadata(metadata);

				File.WriteAllBytes(minimapFile, minimap);
				File.WriteAllBytes(heightMapFile, heightmap);
				File.WriteAllBytes(metalMapFile, metalmap);
				File.WriteAllBytes(metadataPath, metadata);

				lock (mapRequestsLock)
				{
					List<MapRequestCallBacks> rl;
					currentMapRequests.TryGetValue(mapName, out rl);
					if (rl != null) foreach (var request in rl) request.SuccessCallback(map, minimap, heightmap, metalmap);
					currentMapRequests.Remove(mapName);
				}
			}
			catch (Exception e)
			{
				Trace.WriteLine("Unable to deserialize map " + mapName + " from the server: " + e);

				try
				{
					lock (mapRequestsLock)
					{
						List<MapRequestCallBacks> rl;
						currentMapRequests.TryGetValue(mapName, out rl);
						if (rl != null) foreach (var request in rl) request.ErrorCallback(e);
						currentMapRequests.Remove(mapName);
					}
				}
				catch (Exception ex)
				{
					Trace.TraceError("Error processing map download error {0}: {1}", mapName, ex);
				}
			}
		}

		void GetMod(string modName, Action<Mod> callback, Action<Exception> errorCallback)
		{
			var modPath = GetMetadataPath(modName);

			if (File.Exists(modPath))
			{
				// mod found
				Mod mod = null;
				try
				{
					mod = GetModMetadata(modName);
				}
				catch (Exception e)
				{
					Trace.WriteLine("Unable to deserialize mod " + modName + " from disk: " + e);
				}
				if (mod != null)
				{
					callback(mod);
					return;
				}
			}
			lock (modRequestsLock)
			{
				List<ModRequestCallBacks> list;
				if (currentModRequests.TryGetValue(modName, out list))
				{
					list.Add(new ModRequestCallBacks(callback, errorCallback));
					return;
				}
				currentModRequests[modName] = new List<ModRequestCallBacks> { new ModRequestCallBacks(callback, errorCallback) };
			}
			try
			{
				byte[] modData;
				lock (webClientForMod)
				{
					modData = webClientForMod.DownloadData(String.Format("{0}/{1}.metadata.xml.gz", ServerResourceUrlBase, modName.EscapePath()));
				}

				var mod = GetModMetadata(modData);

				File.WriteAllBytes(GetMetadataPath(modName), modData);

				lock (modRequestsLock)
				{
					List<ModRequestCallBacks> rl;
					currentModRequests.TryGetValue(modName, out rl);
					if (rl != null) foreach (var request in rl) request.SuccessCallback(mod);
					currentModRequests.Remove(modName);
				}
			}
			catch (Exception e)
			{
				Trace.WriteLine("Unable to deserialize mod " + modName + " from the server: " + e);

				try
				{
					lock (modRequestsLock)
					{
						List<ModRequestCallBacks> rl;
						currentModRequests.TryGetValue(modName, out rl);
						if (rl != null) foreach (var request in rl) request.ErrorCallback(e);
						currentModRequests.Remove(modName);
					}
				}
				catch (Exception ex)
				{
					Trace.TraceError("Error processing mod download error {0}: {1}", modName, ex);
				}
			}
		}


		class MapRequestCallBacks
		{
			public readonly Action<Exception> ErrorCallback;
			public readonly MapCallback SuccessCallback;

			public MapRequestCallBacks(MapCallback successCallback, Action<Exception> errorCallback)
			{
				SuccessCallback = successCallback;
				ErrorCallback = errorCallback;
			}
		}

		class ModRequestCallBacks
		{
			public readonly Action<Exception> ErrorCallback;
			public readonly Action<Mod> SuccessCallback;

			public ModRequestCallBacks(Action<Mod> successCallback, Action<Exception> errorCallback)
			{
				SuccessCallback = successCallback;
				ErrorCallback = errorCallback;
			}
		}
	}
}