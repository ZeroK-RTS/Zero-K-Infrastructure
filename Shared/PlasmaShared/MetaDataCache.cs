#region using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Serialization;
using PlasmaShared;
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
        readonly WebClient webClientForMap = new WebClient() { Proxy = null };
        readonly WebClient webClientForMod = new WebClient() { Proxy = null };

        public MetaDataCache(SpringPaths springPaths)
        {
            resourceFolder = Utils.MakePath(springPaths.Cache, "Resources");
            Utils.CheckPath(resourceFolder);
        }

        public List<ResourceData> FindResourceData(string[] words, ResourceType? type)
        {
            var cs = GlobalConst.GetContentService();
            return cs.Query(new FindResourceDataRequest() { Words = words, Type = type }).Resources;
        }

        public string GetHeightmapPath(string name)
        {
            return string.Format("{0}/{1}.heightmap.jpg", resourceFolder, name.EscapePath());
        }

        public void GetMap(string mapName, MapCallback callback, Action<Exception> errorCallback)
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
                    var serverResourceUrlBase = GlobalConst.ResourceBaseUrl;
                    minimap = webClientForMap.DownloadData(String.Format("{0}/{1}.minimap.jpg", serverResourceUrlBase, mapName.EscapePath()));

                    metalmap = webClientForMap.DownloadData(String.Format("{0}/{1}.metalmap.jpg", serverResourceUrlBase, mapName.EscapePath()));

                    heightmap = webClientForMap.DownloadData(String.Format("{0}/{1}.heightmap.jpg", serverResourceUrlBase, mapName.EscapePath()));

                    metadata = webClientForMap.DownloadData(String.Format("{0}/{1}.metadata.xml.gz", serverResourceUrlBase, mapName.EscapePath()));
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

        public void GetMapAsync(string mapName, MapCallback callback, Action<Exception> errorCallback)
        {
            Utils.StartAsync(() => GetMap(mapName, callback, errorCallback));
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

        public void GetMod(string modName, Action<Mod> callback, Action<Exception> errorCallback)
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
                    modData = webClientForMod.DownloadData(String.Format("{0}/{1}.metadata.xml.gz", GlobalConst.ResourceBaseUrl, modName.EscapePath()));
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

        public void GetModAsync(string modName, Action<Mod> callback, Action<Exception> errorCallback)
        {
            Utils.StartAsync(() => GetMod(modName, callback, errorCallback));
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

        public static byte[] SerializeAndCompressMetaData(ResourceInfo info)
        {
            var serializedStream = new MemoryStream();
            new XmlSerializer(info.GetType()).Serialize(serializedStream, info);
            serializedStream.Position = 0;
            return serializedStream.ToArray().Compress();
        }

        Map GetMapMetadata(string name)
        {
            var data = File.ReadAllBytes(GetMetadataPath(name));
            return GetMapMetadata(data);
        }

        static Map GetMapMetadata(byte[] data)
        {
            var ret = (Map)new XmlSerializer(typeof(Map)).Deserialize(new MemoryStream(data.Decompress()));
            ret.Name = ret.Name.Replace(".smf", ""); // hack remove this after server data reset

            return ret;
        }

        static Mod GetModMetadata(byte[] data)
        {
            var ret = (Mod)new XmlSerializer(typeof(Mod)).Deserialize(new MemoryStream(data.Decompress()));

            if (ret.Options != null) foreach (var option in ret.Options) if (option.Type == OptionType.Number) option.Default = option.Default.Replace(",", ".");
            return ret;
        }

        Mod GetModMetadata(string name)
        {
            var data = File.ReadAllBytes(GetMetadataPath(name));
            return GetModMetadata(data);
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


        public static Mod ServerGetMod(string internalName)
        {
            var data = ServerGetMetaData(internalName);
            return data == null ? null : GetModMetadata(data);
        }
        
        public static Map ServerGetMap(string internalName)
        {
            var data = ServerGetMetaData(internalName);
            return data == null ? null : GetMapMetadata(data);
        }

        /// <summary>
        /// A resource's metadata: off the site's disk when that is reachable, otherwise off the
        /// site itself over HTTP.
        ///
        /// **The disk read is an optimisation, not the design.** It works because the lobby
        /// server used to run inside the website's IIS worker, sharing both its process and its
        /// filesystem. Out of process it inherits neither: GlobalConst.SiteDiskPath falls back to
        /// a developer's checkout path on Windows and to nothing at all elsewhere, and both
        /// lookups then returned null silently - which a player sees as "this game has no
        /// options" from !listoptions.
        ///
        /// The fallback needs no new API. The website already PUBLISHES these exact bytes at
        /// {ResourceBaseUrl}/{name}.metadata.xml.gz - the game client downloads them from there,
        /// in this same file, and parses them with the same two methods. A second copy of that
        /// over ILobbyServerApi would have been a new endpoint for a file that already has a URL,
        /// and ILobbyServerApi runs website-to-lobby-server, which is the wrong direction for
        /// this anyway.
        ///
        /// Cached by name because an InternalName identifies one immutable version of a resource
        /// - a new game build is a new row with a new name - and ServerBattle asks on every
        /// battle it opens. Only successes are cached, so a website that was briefly down does
        /// not stay down for the life of the process.
        /// </summary>
        static byte[] ServerGetMetaData(string internalName)
        {
            if (string.IsNullOrEmpty(internalName)) return null;

            byte[] cached;
            if (serverMetaData.TryGetValue(internalName, out cached)) return cached;

            var escaped = internalName.EscapePath();

            if (!string.IsNullOrEmpty(GlobalConst.SiteDiskPath))
            {
                var file = Path.Combine(GlobalConst.SiteDiskPath, GlobalConst.ResourceFolder, $"{escaped}.metadata.xml.gz");
                if (File.Exists(file))
                {
                    var bytes = File.ReadAllBytes(file);
                    serverMetaData[internalName] = bytes;
                    return bytes;
                }
            }

            var url = $"{GlobalConst.ResourceBaseUrl}/{escaped}.metadata.xml.gz";
            try
            {
                byte[] bytes;
                lock (serverWebClient) bytes = serverWebClient.DownloadData(url);
                serverMetaData[internalName] = bytes;
                return bytes;
            }
            catch (Exception ex)
            {
                // Once per resource, not once per battle. A whole site being unreachable is one
                // line per resource anyone tries to host, which is the number worth seeing.
                WarnOnce(internalName,
                    $"No metadata for {internalName}: not on disk at "
                    + $"{(string.IsNullOrEmpty(GlobalConst.SiteDiskPath) ? "<SiteDiskPath unset>" : GlobalConst.SiteDiskPath)}"
                    + $" and {url} did not answer ({ex.Message}). Its options will look empty.");
                return null;
            }
        }

        static readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte[]> serverMetaData =
            new System.Collections.Concurrent.ConcurrentDictionary<string, byte[]>();

        /// <summary>
        /// Ten seconds, because this is called while a battle is opening. WebClient's default is
        /// a hundred, and a website that hangs rather than refuses would hold up every battle in
        /// the lobby for that long, one resource at a time.
        /// </summary>
        class TimeoutWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri address)
            {
                var request = base.GetWebRequest(address);
                if (request != null) request.Timeout = 10000;
                return request;
            }
        }

        static readonly WebClient serverWebClient = new TimeoutWebClient { Proxy = null };

        static readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte> warned =
            new System.Collections.Concurrent.ConcurrentDictionary<string, byte>();

        static void WarnOnce(string key, string message)
        {
            if (warned.TryAdd(key, 0)) Trace.TraceWarning(message);
        }

        /// <summary>Forgets what it downloaded. For tests, which change where the site is.</summary>
        public static void ServerClearMetaDataCache()
        {
            serverMetaData.Clear();
            warned.Clear();
        }
        
    }
}