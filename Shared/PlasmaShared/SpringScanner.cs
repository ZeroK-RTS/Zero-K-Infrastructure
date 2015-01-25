#region using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using MonoTorrent.Common;
using Newtonsoft.Json;
using PlasmaShared;
using ZkData.UnitSyncLib;

#endregion

namespace ZkData
{
    public class SpringScanner: IDisposable
    {
        /// <summary>
        /// auto save cache every X seconds if dirty
        /// </summary>
        const int DirtyCacheSave = 30;

        /// <summary>
        /// Files with different Extensions as these are ignored
        /// </summary>
        static readonly string[] Extensions = { ".sd7", ".sdz", ".sdp" };

        /// <summary>
        ///  max size of a minimap to send to the server
        /// </summary>
        const int ImageSize = 256;

        const int MaximumConcurrentTransmissions = 5;

        /// <summary>
        ///  if version is smaller than the version in the server, the uploaded data is refused
        /// </summary>
        const int PlasmaServiceVersion = 3;

        /// <summary>
        /// how long to wait (seconds) before asking server for same resource
        /// </summary>
        const int RescheduleServerQuery = 120;


        /// <summary>
        /// How long to wait to reask server if unitsync is missing
        /// </summary>
        const int UnitsyncMissingReaskQuery = 600;

        /// <summary>
        /// time between work item operations in ms
        /// </summary>
        const int ScannerCycleTime = 1000;

        /// <summary>
        /// items before unitsync reinitialization
        /// </summary>
        const int UnitSyncReInitFrequency = 50;

        CacheFile cache = new CacheFile();

        /// <summary>
        /// Path of the file containing the serialized cache
        /// </summary>
        readonly string cachePath;

        /// <summary>
        /// Whether the cache need to be saved
        /// </summary>
        bool isCacheDirty;

        /// <summary>
        /// number of work items being sent
        /// </summary>
        int itemsSending;

        DateTime lastCacheSave;

        /// <summary>
        /// unitsync is run on this thread
        /// </summary>
        Thread mainThread;

        /// <summary>
        /// looks for changes in the maps folder
        /// </summary>
        readonly List<FileSystemWatcher> mapsWatchers = new List<FileSystemWatcher>();

        /// <summary>
        /// looks for changes in the mods folder
        /// </summary>
        readonly List<FileSystemWatcher> modsWatchers = new List<FileSystemWatcher>();

        /// <summary>
        /// looks for changes in packages folder
        /// </summary>
        readonly List<FileSystemWatcher> packagesWatchers = new List<FileSystemWatcher>();

        readonly IContentService service = GlobalConst.GetContentService();

        readonly SpringPaths springPaths;

        public UnitSync unitSync;

        /// <summary>
        /// whether an attempt to load unitsync was performed
        /// </summary>
        string unitSyncAttemptedFolder;

        /// <summary>
        /// number of unitsync operations since the last unitsync initialization
        /// </summary>
        int unitSyncReInitCounter;


        /// <summary>
        /// queue of items to process
        /// </summary>
        readonly LinkedList<WorkItem> workQueue = new LinkedList<WorkItem>();
        int workTotal;

        public bool UseUnitSync = Environment.OSVersion.Platform != PlatformID.Unix; // unitsync fails on linux


        public MetaDataCache MetaData { get; private set; }


        public bool WatchingEnabled { get { return mapsWatchers.First().EnableRaisingEvents; } set { foreach (var watcher in mapsWatchers.Concat(modsWatchers).Concat(packagesWatchers)) watcher.EnableRaisingEvents = value; } }

        public event EventHandler<ResourceChangedEventArgs> LocalResourceAdded = delegate { };
        public event EventHandler<ResourceChangedEventArgs> LocalResourceRemoved = delegate { };
        public static event EventHandler<MapRegisteredEventArgs> MapRegistered = delegate { };
        public static event EventHandler<EventArgs<Mod>> ModRegistered = delegate { };
        public event EventHandler<ProgressEventArgs> WorkProgressChanged = delegate { };
        public event EventHandler<ProgressEventArgs> WorkStarted = delegate { };
        public event EventHandler WorkStopped = delegate { };
        public event EventHandler<CancelEventArgs<IResourceInfo>> UploadUnitsyncData = delegate { }; // raised before attempting to upload unitsync data
        public event EventHandler<CancelEventArgs<CacheItem>> RetryResourceCheck = delegate { }; // raised before attempting to reconnect to server to check for resource info

        public SpringScanner(SpringPaths springPaths)
        {
            this.springPaths = springPaths;
            MetaData = new MetaDataCache(springPaths, this);

            foreach (var folder in springPaths.DataDirectories)
            {
                var modsPath = Utils.MakePath(folder, "games");
                if (Directory.Exists(modsPath)) modsWatchers.Add(new FileSystemWatcher(modsPath));
                var mapsPath = Utils.MakePath(folder, "maps");
                if (Directory.Exists(mapsPath)) mapsWatchers.Add(new FileSystemWatcher(mapsPath));
                var packagesPath = Utils.MakePath(folder, "packages");
                if (Directory.Exists(packagesPath)) packagesWatchers.Add(new FileSystemWatcher(packagesPath));
            }

            SetupWatcherEvents(mapsWatchers);
            SetupWatcherEvents(modsWatchers);
            SetupWatcherEvents(packagesWatchers);

            Directory.CreateDirectory(springPaths.Cache);
            cachePath = Utils.MakePath(springPaths.Cache, "ScannerCache.json");
            Directory.CreateDirectory(Utils.MakePath(springPaths.Cache, "Resources"));
        }

        ~SpringScanner()
        {
            Dispose();
        }

        public void Dispose()
        {
            WatchingEnabled = false;
            isDisposed = true;
            if (unitSync != null) unitSync.Dispose(); //(visual studio recommend a dispose)
            unitSync = null;
            if (isCacheDirty) SaveCache();
            GC.SuppressFinalize(this);
        }

        bool isDisposed;

        public CacheItem FindCacheEntry(string name, int springHash)
        {
            lock (cache)
            {
                CacheItem item;
                if (cache.NameIndex.TryGetValue(name, out item))
                {
                    return item;
                }
            }
            return null;
        }


        public int GetSpringHash(string name, string springVersion)
        {
            lock (cache)
            {
                springVersion = springVersion ?? springPaths.SpringVersion;
                CacheItem item;
                if (cache.NameIndex.TryGetValue(name, out item))
                {
                    SpringHashEntry match;
                    if (string.IsNullOrEmpty(springVersion)) match = item.SpringHash.LastOrDefault();
                    else match = item.SpringHash.FirstOrDefault(x => x.SpringVersion == springVersion);
                    if (match != null) return match.SpringHash;
                }
            }
            return 0;
        }


        public bool HasResource(string name)
        {
            if (mainThread != null)
            {
                // scanner active
                return cache.NameIndex.ContainsKey(name);
            }
            else
            {
                VerifyUnitSync();

                if (unitSync != null)
                {
                    if (unitSync.GetMapNames().Any(x => x == name)) return true;
                    if (unitSync.GetModNames().Any(x => x == name)) return true;
                }
                return false;
            }
        }

        public List<CacheItem> GetAllMapResource()
        {
            var mapList = new List<CacheItem>();
            foreach (var map in cache.NameIndex) if (map.Value.ResourceType == ResourceType.Map) mapList.Add(map.Value);
            return mapList;
        }

        public List<CacheItem> GetAllModResource()
        {
            var modList = new List<CacheItem>();
            foreach (var mod in cache.NameIndex) if(mod.Value.ResourceType==ResourceType.Mod) modList.Add(mod.Value);
            return modList;
        }

        public void Start()
        {
            WatchingEnabled = true;
            mainThread = Utils.SafeThread(MainThreadFunction);
            mainThread.Priority = ThreadPriority.BelowNormal;
            mainThread.Start();
        }


        void AddWork(string folder, string file, WorkItem.OperationType operationType, DateTime executeOn, bool toFront)
        {
            AddWork(new CacheItem { Folder = folder, FileName = file }, operationType, executeOn, toFront);
        }


        void AddWork(CacheItem item, WorkItem.OperationType operationType, DateTime executeOn, bool toFront)
        {
            workTotal++;
            lock (workQueue)
            {
                var work = new WorkItem(item, operationType, executeOn);
                work.CacheItem = item;
                if (toFront) workQueue.AddFirst(work);
                else workQueue.AddLast(work);
            }
        }


        void CacheItemAdd(CacheItem item)
        {
            lock (cache)
            {
                cache.ShortPathIndex[item.ShortPath] = item;
                cache.HashIndex[item.Md5] = item;
                cache.NameIndex[item.InternalName] = item;
                cache.FailedUnitSyncFiles.Remove(item.ShortPath);
                LocalResourceAdded(this, new ResourceChangedEventArgs(item));
                isCacheDirty = true;
            }
        }

        void CacheItemRemove(CacheItem item)
        {
            lock (cache)
            {
                cache.ShortPathIndex.Remove(item.ShortPath);
                cache.HashIndex.Remove(item.Md5);
                cache.NameIndex.Remove(item.InternalName);
                LocalResourceRemoved(this, new ResourceChangedEventArgs(item));
                isCacheDirty = true;
            }
        }

        void CacheMarkFailedUnitSync(string shortpath)
        {
            lock (cache)
            {
                cache.FailedUnitSyncFiles[shortpath] = true;
                isCacheDirty = true;
            }
        }


        string GetFullPath(WorkItem work)
        {
            string fullPath = null;
            foreach (var directory in springPaths.DataDirectories)
            {
                var path = Utils.MakePath(directory, work.CacheItem.ShortPath);
                if (File.Exists(path))
                {
                    fullPath = path;
                    break;
                }
            }
            return fullPath;
        }


        WorkItem GetNextWorkItem()
        {
            var now = DateTime.Now;
            lock (workQueue)
            {
                var queue = itemsSending > MaximumConcurrentTransmissions
                                ? workQueue.Where(item => item.Operation != WorkItem.OperationType.UnitSync)
                                : workQueue;
                foreach (var item in queue)
                {
                    if (item.ExecuteOn > now) continue; // do it later
                    workQueue.Remove(item);
                    return item;
                }
            }
            return null;
        }

        void GetResourceData(WorkItem work)
        {

            ResourceData result = null;
            try
            {
                result = service.GetResourceData(work.CacheItem.Md5.ToString(), work.CacheItem.InternalName);
            }
            catch (Exception ex)
            {
                var args = new CancelEventArgs<CacheItem>(work.CacheItem);
                RetryResourceCheck.Invoke(this, args);
                if (!args.Cancel)
                {
                    Trace.TraceError("Error getting resource data: {0}", ex);
                    AddWork(work.CacheItem, WorkItem.OperationType.ReAskServer, DateTime.Now.AddSeconds(RescheduleServerQuery), false);
                    return;
                }
            }

            if (result == null)
            {
                if (!UseUnitSync || springPaths.SpringVersion == null)
                {
                    Trace.WriteLine(String.Format("No server resource data for {0}, asking later", work.CacheItem.ShortPath));
                    AddWork(work.CacheItem, WorkItem.OperationType.ReAskServer, DateTime.Now.AddSeconds(UnitsyncMissingReaskQuery), false);
                }
                else {
                    Trace.WriteLine(String.Format("No server resource data for {0}, queing upload", work.CacheItem.ShortPath));
                    AddWork(work.CacheItem, WorkItem.OperationType.UnitSync, DateTime.Now, false);
                }
                return;
            }
            work.CacheItem.InternalName = result.InternalName;
            work.CacheItem.ResourceType = result.ResourceType;
            work.CacheItem.SpringHash = result.SpringHashes.ToArray();
            Trace.WriteLine(string.Format("Adding {0}", work.CacheItem.InternalName));
            CacheItemAdd(work.CacheItem);
        }

        static string GetShortPath(string folder, string file)
        {
            return string.Format("{0}/{1}", folder, Path.GetFileName(file));
        }

        IResourceInfo GetUnitSyncData(string filename)
        {
            IResourceInfo ret = null;
            try
            {
                unitSyncReInitCounter++;
                Trace.TraceInformation("GetUnitSyncData");

                var map = unitSync.GetMapFromArchive(filename);
                if (map != null)
                {
                    ret = map;
                    if (map.Minimap == null || map.Metalmap == null || map.Heightmap == null) throw new Exception("Map bitmap is null");
                }
                else ret = unitSync.GetModFromArchive(filename);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error processing unitsync for {0}: {1}", filename, ex);
            }
            finally
            {
                try
                {
                    unitSync.Reset();
                }
                catch (Exception e)
                {
                    Trace.TraceError("Error resetting unitsync for {0}: {1}", filename, e);
                }
            }
            if (ret != null && ret.Name == null)
            {
                Trace.TraceError("Internal name not found for " + filename);
                ret = null;
            }
            return ret;
        }

        string GetWatcherFolder(FileSystemWatcher watcher)
        {
            if (mapsWatchers.Contains(watcher)) return "maps";
            if (modsWatchers.Contains(watcher)) return "games";
            if (packagesWatchers.Contains(watcher)) return "packages";
            throw new ArgumentException("Invalid watcher", "watcher");
        }

        public int GetWorkCost()
        {
            lock (workQueue)
            {
                return workQueue.Count;
            }
        }


        void InitialFolderScan(string folder, Dictionary<string, bool> foundFiles)
        {
            var fileList = new List<string>();
            foreach (var dd in springPaths.DataDirectories)
            {
                var path = Utils.MakePath(dd, folder);
                if (Directory.Exists(path)) {
                    try {
                        fileList.AddRange(Directory.GetFiles(path));
                    } catch {}
                }
            }

            foreach (var f in fileList)
            {
                if (Extensions.Contains(Path.GetExtension(f)))
                {
                    var shortPath = GetShortPath(folder, Path.GetFileName(f));
                    if (cache.FailedUnitSyncFiles.ContainsKey(shortPath) || foundFiles.ContainsKey(shortPath)) continue;
                    foundFiles.Add(shortPath, true);
                    if (!cache.ShortPathIndex.ContainsKey(shortPath)) AddWork(folder, Path.GetFileName(f), WorkItem.OperationType.Hash, DateTime.Now, false);
                    else if (cache.ShortPathIndex[shortPath].Length != new FileInfo(f).Length)
                    {
                        CacheItemRemove(cache.ShortPathIndex[shortPath]);
                        AddWork(folder, Path.GetFileName(f), WorkItem.OperationType.Hash, DateTime.Now, false);
                    }
                }
            }
        }

        void InitialScan()
        {
            CacheFile loadedCache = null;
            if (File.Exists(cachePath))
            {
                try
                {
                   loadedCache = JsonConvert.DeserializeObject<CacheFile>(File.ReadAllText(cachePath));
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Warning: problem reading scanner cache: {0}", ex);
                    loadedCache = null;
                }
            }

            if (loadedCache != null) cache = loadedCache;

            var foundFiles = new Dictionary<string, bool>();

            InitialFolderScan("games", foundFiles);
            InitialFolderScan("maps", foundFiles);
            InitialFolderScan("packages", foundFiles);

            Dictionary<string, CacheItem> copy;
            lock (cache) copy = new Dictionary<string, CacheItem>(cache.ShortPathIndex);
            foreach (var pair in copy) if (!foundFiles.ContainsKey(pair.Key)) CacheItemRemove(pair.Value);


            Trace.TraceInformation("Initial scan done");
        }


        void MainThreadFunction()
        {
            try {
                InitialScan();
            } catch (Exception ex) {
                Trace.TraceError("Error in scanner initial scan: {0}",ex);
            }

            try
            {
                var isWorking = false;
                var workDone = 0;
                while (!isDisposed)
                {
                    try {
                        Thread.Sleep(ScannerCycleTime);

                        if (isCacheDirty && DateTime.Now.Subtract(lastCacheSave).TotalSeconds > DirtyCacheSave) {
                            lastCacheSave = DateTime.Now;
                            isCacheDirty = false;
                            SaveCache();
                        }

                        WorkItem workItem;
                        while ((workItem = GetNextWorkItem()) != null) {
                            if (isDisposed) return;

                            if (!isWorking) {
                                isWorking = true;
                                workDone = 0;
                                workTotal = GetWorkCost();
                                WorkStarted(this, new ProgressEventArgs(workDone, workTotal, workItem.CacheItem.FileName));
                            }
                            else {
                                workDone++;
                                workTotal = Math.Max(GetWorkCost(), workTotal);
                                WorkProgressChanged(this,
                                                    new ProgressEventArgs(workDone,
                                                                          workTotal,
                                                                          string.Format("{0} {1}", workItem.Operation, workItem.CacheItem.FileName)));
                            }

                            if (workItem.Operation == WorkItem.OperationType.Hash) PerformHashOperation(workItem);
                            if (workItem.Operation == WorkItem.OperationType.UnitSync) {
                                if (springPaths.UnitSyncDirectory != null) PerformUnitSyncOperation(workItem); // if there is no unitsync, retry later
                                else AddWork(workItem.CacheItem, WorkItem.OperationType.UnitSync, DateTime.Now.AddSeconds(RescheduleServerQuery), false);
                            }
                            if (workItem.Operation == WorkItem.OperationType.ReAskServer) GetResourceData(workItem);
                        }
                        if (isWorking) {
                            isWorking = false;
                            WorkStopped(this, EventArgs.Empty);
                        }
                    } catch (Exception ex) {
                        Trace.TraceError("Exception in scanning thread: {0}",ex);
                    }
                }
            }
            finally
            {
                if (unitSync != null) unitSync.Dispose();
                unitSync = null;
            }
        }


        void PerformHashOperation(WorkItem work)
        {
            string fullPath = null;
            try
            {
                fullPath = GetFullPath(work);
                if (fullPath == null) throw new Exception("workitem file not found");

                using (var fs = File.OpenRead(fullPath)) work.CacheItem.Md5 = Hash.HashStream(fs);
                work.CacheItem.Length = (int)new FileInfo(fullPath).Length;

                if (!cache.HashIndex.ContainsKey(work.CacheItem.Md5)) GetResourceData(work);
            }
            catch (Exception e)
            {
                Trace.WriteLine("Can't hash " + work.CacheItem.ShortPath + " (" + e + ")");
            }
        }


        void PerformUnitSyncOperation(WorkItem workItem)
        {
            Trace.TraceInformation("PerformUnitSyncOperation");
            VerifyUnitSync();

            if (unitSync == null)
            {
                Trace.TraceError("Skipping file after unitsync loading errors: {0}", workItem.CacheItem.ShortPath);
                CacheMarkFailedUnitSync(workItem.CacheItem.ShortPath);
                return;
            }

            var info = GetUnitSyncData(workItem.CacheItem.FileName);
            UnInitUnitsync();

            if (info != null)
            {
                workItem.CacheItem.InternalName = info.Name;
                workItem.CacheItem.ResourceType = info is Map ? ResourceType.Map : ResourceType.Mod;
                var hashes = new List<SpringHashEntry>();
                if (workItem.CacheItem.SpringHash != null) hashes.AddRange(workItem.CacheItem.SpringHash.Where(x => x.SpringVersion != springPaths.SpringVersion));
                hashes.Add(new SpringHashEntry() { SpringHash = info.Checksum, SpringVersion = springPaths.SpringVersion });
                workItem.CacheItem.SpringHash = hashes.ToArray();

                CacheItemAdd(workItem.CacheItem);

                var args = new CancelEventArgs<IResourceInfo>(info);
                UploadUnitsyncData.Invoke(this,args);
                if (args.Cancel) return;

                var serializedData = MetaDataCache.SerializeAndCompressMetaData(info);

                var map = info as Map;
                object userState = null;
                try
                {
                    var creator = new TorrentCreator();
                    creator.Path = GetFullPath(workItem);
                    var ms = new MemoryStream();
                    creator.Create(ms);

                    byte[] minimap = null;
                    byte[] metalMap = null;
                    byte[] heightMap = null;
                    if (map != null)
                    {
                        minimap = map.Minimap.ToBytes(ImageSize);
                        metalMap = map.Metalmap.ToBytes(ImageSize);
                        heightMap = map.Heightmap.ToBytes(ImageSize);
                        userState = new MapRegisteredEventArgs(info.Name, map, minimap, metalMap, heightMap, serializedData);
                    }
                    var mod = info as Mod;
                    if (mod != null) userState = new KeyValuePair<Mod, byte[]>(mod, serializedData);

                    Trace.TraceInformation("uploading {0} to server", info.Name);
                    Task.Factory.StartNew(() => {
                        ReturnValue e;
                        try {
                             e = service.RegisterResource(PlasmaServiceVersion, springPaths.SpringVersion, workItem.CacheItem.Md5.ToString(),
                                workItem.CacheItem.Length, info is Map ? ResourceType.Map : ResourceType.Mod, workItem.CacheItem.FileName, info.Name,
                                info.Checksum, serializedData, mod != null ? mod.Dependencies.ToList() : null, minimap, metalMap, heightMap,
                                ms.ToArray());
                        } catch (Exception ex) {
                            Trace.TraceError("Error uploading data to server: {0}", ex);
                            return;
                        } finally {
                            Interlocked.Decrement(ref itemsSending);
                        }
                        
                        if (e != ReturnValue.Ok)
                        {
                            Trace.TraceWarning("Resource registering failed: {0}", e);
                            return;
                        }
                        var mapArgs = userState as MapRegisteredEventArgs;
                        if (mapArgs != null)
                        {
                            var mapName = mapArgs.MapName;
                            MetaData.SaveMinimap(mapName, mapArgs.Minimap);
                            MetaData.SaveMetalmap(mapName, mapArgs.MetalMap);
                            MetaData.SaveHeightmap(mapName, mapArgs.HeightMap);
                            MetaData.SaveMetadata(mapName, mapArgs.SerializedData);
                            MapRegistered(this, mapArgs);
                        }
                        else
                        {
                            var kvp = (KeyValuePair<Mod, byte[]>)userState;
                            var modInfo = kvp.Key;
                            var serializedDataRet = kvp.Value;
                            MetaData.SaveMetadata(modInfo.Name, serializedDataRet);
                            ModRegistered(this, new EventArgs<Mod>(mod));
                        }
                    });

                    Interlocked.Increment(ref itemsSending);
                }
                catch (Exception e)
                {
                    Trace.TraceError("Error registering new resource {0}: {1}", workItem.CacheItem.ShortPath, e);
                }
            }
            else
            {
                Trace.TraceError("Could not unitsync file {0}", workItem.CacheItem.ShortPath);
                CacheMarkFailedUnitSync(workItem.CacheItem.ShortPath);
            }
            return;
        }


        void SaveCache()
        {
            lock (cache) {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(cachePath));
                    File.WriteAllText(cachePath, JsonConvert.SerializeObject(cache));
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error saving springscanner cache: {0}", ex);
                }
            }
        }

        void SetupWatcherEvents(IEnumerable<FileSystemWatcher> watchers)
        {
            foreach (var watcher in watchers)
            {
                watcher.IncludeSubdirectories = true;
                watcher.Created += HandleWatcherChange;
                watcher.Changed += HandleWatcherChange;
                watcher.Deleted += HandleWatcherChange;
                watcher.Renamed += HandleWatcherChange;
            }
        }

        /// <summary>UnInitUnitsync() check whether SpringScanner have any more mods/map to Unitsynced and call unitsync's UnInit() when there's no more work.
        /// </summary>
        public void UnInitUnitsync()
        {
           //upon completion of any work: dispose unitsync. It can be re-initialize again later by VerifyUnitSync()
            if (unitSync != null && GetWorkCost() < 1)
            {
                try
                {
                    unitSync.Dispose();
                    unitSync = null;
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Error disposing unitsync: {0}", ex);
                }
            }
        } 

        /// <summary>VerifyUnitSync() check whether unitSync should be initialized and perform unitSync initialization.
        /// </summary> 
        public void VerifyUnitSync()
        {
            if (unitSyncReInitCounter >= UnitSyncReInitFrequency)
            {
                unitSyncAttemptedFolder = null;
                unitSyncReInitCounter = 0;
            }
            if (unitSyncAttemptedFolder != springPaths.UnitSyncDirectory || unitSync==null)
            {
                if (unitSync != null)
                {
                    try
                    {
                        unitSync.Dispose();
                        unitSync = null;
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceWarning("Error disposing unitsync: {0}", ex);
                    }
                }
                unitSyncAttemptedFolder = springPaths.UnitSyncDirectory;
                try
                {
                    unitSync = new UnitSync(springPaths);
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Error initializing unitsync: {0}", ex);
                }
            }
        }


        void HandleWatcherChange(object sender, FileSystemEventArgs e)
        {
            if (!Extensions.Contains(Path.GetExtension(e.Name))) return;

            var folder = GetWatcherFolder((FileSystemWatcher)sender);
            var shortPath = GetShortPath(folder, e.Name);
            CacheItem item;
            if (e.ChangeType == WatcherChangeTypes.Deleted)
            {
                if (cache.ShortPathIndex.TryGetValue(shortPath, out item)) CacheItemRemove(item);
            }
            else
            {
                // changed, created, renamed
                // remove the item if present in the cache, then process the item
                if (cache.ShortPathIndex.TryGetValue(shortPath, out item)) CacheItemRemove(item);
                unitSyncReInitCounter = UnitSyncReInitFrequency + 1; // force unitsync re-init
                AddWork(folder, e.Name, WorkItem.OperationType.Hash, DateTime.Now, true);
            }
        }


        [Serializable]
        class CacheFile
        {
            public Dictionary<string, bool> FailedUnitSyncFiles = new Dictionary<string, bool>();
            public Dictionary<Hash, CacheItem> HashIndex = new Dictionary<Hash, CacheItem>();
            public Dictionary<string, CacheItem> NameIndex = new Dictionary<string, CacheItem>();
            public Dictionary<string, CacheItem> ShortPathIndex = new Dictionary<string, CacheItem>();
            public string SpringVersion;
        }


        [Serializable]
        public class CacheItem
        {
            public string FileName;
            public string Folder;

            public string InternalName;
            public int Length;
            public Hash Md5;
            public ResourceType ResourceType;

            public string ShortPath { get { return GetShortPath(Folder, FileName); } }

            public SpringHashEntry[] SpringHash;
        }


        public class ResourceChangedEventArgs: EventArgs
        {
            public CacheItem Item { get; protected set; }

            public ResourceChangedEventArgs(CacheItem item)
            {
                Item = item;
            }
        }


        class WorkItem
        {
            public enum OperationType
            {
                Hash,
                ReAskServer,
                UnitSync
            }


            public CacheItem CacheItem;
            public DateTime ExecuteOn;
            public readonly OperationType Operation;


            public WorkItem(CacheItem item, OperationType operation, DateTime executeOn)
            {
                CacheItem = item;
                ExecuteOn = executeOn;
                Operation = operation;
            }
        }
    }

    public class ProgressEventArgs: EventArgs
    {
        public int WorkDone { get; private set; }
        public string WorkName { get; private set; }
        public int WorkTotal { get; private set; }

        public ProgressEventArgs(int workDone, int workTotal, string workName)
        {
            WorkTotal = workTotal;
            WorkDone = workDone;
            WorkName = workName;
        }
    }
}