#region using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Serialization;
using PlasmaShared.ContentService;
using ZkData.UnitSyncLib;

#endregion

namespace ZkData
{
    public class MetaDataCache
    {
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
        public bool UseSpringHashes = false;

        public MetaDataCache(SpringPaths springPaths, SpringScanner scanner)
        {
            this.scanner = scanner;
            resourceFolder = Utils.MakePath(springPaths.Cache, "Resources");
            Utils.CheckPath(resourceFolder);
        }

        public ResourceData[] FindResourceData(string[] words, ResourceType? type)
        {
            var cs = new ContentService();
            return cs.FindResourceData(words, type);
        }

        public string GetHeightmapPath(string name)
        {
            return string.Format("{0}/{1}.heightmap.jpg", resourceFolder, name.EscapePath());
        }

        public void GetMap(string mapName, MapCallback callback, Action<Exception> errorCallback, string springVersion)
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
                    map = GetMapMetadata(mapName, springVersion);
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
                    var serverResourceUrlBase = GlobalConst.ResourceBaseUrl;
                    minimap = webClientForMap.DownloadData(String.Format("{0}/{1}.minimap.jpg", serverResourceUrlBase, mapName.EscapePath()));

                    metalmap = webClientForMap.DownloadData(String.Format("{0}/{1}.metalmap.jpg", serverResourceUrlBase, mapName.EscapePath()));

                    heightmap = webClientForMap.DownloadData(String.Format("{0}/{1}.heightmap.jpg", serverResourceUrlBase, mapName.EscapePath()));

                    metadata = webClientForMap.DownloadData(String.Format("{0}/{1}.metadata.xml.gz", serverResourceUrlBase, mapName.EscapePath()));
                }

                var map = GetMapMetadata(metadata, springVersion);

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

        public void GetMapAsync(string mapName, MapCallback callback, Action<Exception> errorCallback, string springVersion)
        {
            Utils.StartAsync(() => GetMap(mapName, callback, errorCallback, springVersion));
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

        public void GetMod(string modName, Action<Mod> callback, Action<Exception> errorCallback, string springVersion)
        {
            var modPath = GetMetadataPath(modName);

            if (File.Exists(modPath))
            {
                // mod found
                Mod mod = null;
                try
                {
                    mod = GetModMetadata(modName, springVersion);
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
                    modData = webClientForMod.DownloadData(String.Format("{0}/{1}.metadata.xml.gz", GlobalConst.ResourceBaseUrl, modName.EscapePath()));
                }

                var mod = GetModMetadata(modData, springVersion);

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

        public void GetModAsync(string modName, Action<Mod> callback, Action<Exception> errorCallback, string springVersion)
        {
            Utils.StartAsync(() => GetMod(modName, callback, errorCallback, springVersion));
        }

        public ResourceData GetResourceDataByInternalName(string name)
        {
            try
            {
                var cs = new ContentService();
                return cs.GetResourceDataByInternalName(name);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning(string.Format("Error getting data for resource {0} : {1}", name, ex));
                return null;
            }
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

        Map GetMapMetadata(string name, string springVersion)
        {
            var data = File.ReadAllBytes(GetMetadataPath(name));
            return GetMapMetadata(data, springVersion);
        }

        Map GetMapMetadata(byte[] data, string springVersion)
        {
            var ret = (Map)new XmlSerializer(typeof(Map)).Deserialize(new MemoryStream(data.Decompress()));
            ret.Name = ret.Name.Replace(".smf", ""); // hack remove this after server data reset

            if (UseSpringHashes)
            {
                if (scanner != null)
                {
                    var hash = scanner.GetSpringHash(ret.Name, springVersion);
                    ret.Checksum = hash;
                }
                else
                {
                    var cs = new ContentService();
                    try
                    {
                        var rd = cs.GetResourceDataByInternalName(ret.Name);
                        ret.Checksum = rd.SpringHashes.Single(x => x.SpringVersion == springVersion).SpringHash;
                    }
                    catch (Exception ex)
                    {
                        ret.Checksum = 0;
                        Trace.TraceWarning(string.Format("Failed to get ResourcedData for {0}: {1}", ret.Name, ex));
                    }
                }
            }
            else ret.Checksum = 0;

            return ret;
        }

        Mod GetModMetadata(byte[] data, string springVersion)
        {
            var ret = (Mod)new XmlSerializer(typeof(Mod)).Deserialize(new MemoryStream(data.Decompress()));

            if (UseSpringHashes)
            {
                if (scanner != null)
                {
                    var hash = scanner.GetSpringHash(ret.Name, springVersion);
                    ret.Checksum = hash;
                }
                else
                {
                    var cs = new ContentService();
                    try
                    {
                        var rd = cs.GetResourceDataByInternalName(ret.Name);
                        ret.Checksum = rd.SpringHashes.Single(x => x.SpringVersion == springVersion).SpringHash;
                    }
                    catch (Exception ex)
                    {
                        ret.Checksum = 0;
                        Trace.TraceWarning(string.Format("Failed to get ResourcedData for {0}: {1}", ret.Name, ex));
                    }
                }
            }
            else ret.Checksum = 0;

            if (ret.Options != null) foreach (var option in ret.Options) if (option.Type == OptionType.Number) option.Default = option.Default.Replace(",", ".");
            return ret;
        }

        Mod GetModMetadata(string name, string springVersion)
        {
            var data = File.ReadAllBytes(GetMetadataPath(name));
            return GetModMetadata(data, springVersion);
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