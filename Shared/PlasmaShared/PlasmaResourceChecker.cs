using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using PlasmaShared;
using ZkData.UnitSyncLib;

namespace ZkData
{
    public class PlasmaResourceChecker : IDisposable, IResourcePresenceChecker
    {
        /// <summary>
        ///     auto save cache every X seconds if dirty
        /// </summary>
        private const int DirtyCacheSave = 30;

        private const int MaximumConcurrentTransmissions = 5;


        /// <summary>
        ///     How long to wait to reask server if unitsync is missing
        /// </summary>
        private const int UnitsyncMissingReaskQuery = 600;

        /// <summary>
        ///     how long to wait (seconds) before asking server for same resource
        /// </summary>
        private const int RescheduleServerQuery = 120;


        /// <summary>
        ///     time between work item operations in ms
        /// </summary>
        private const int ScannerCycleTime = 1000;

        /// <summary>
        ///     Files with different Extensions as these are ignored
        /// </summary>
        private static readonly string[] Extensions = { ".sd7", ".sdz", ".sdp" };

        /// <summary>
        ///     Path of the file containing the serialized cache
        /// </summary>
        private readonly string cachePath;

        /// <summary>
        ///     looks for changes in the maps folder
        /// </summary>
        private readonly List<FileSystemWatcher> mapsWatchers = new List<FileSystemWatcher>();

        /// <summary>
        ///     looks for changes in the mods folder
        /// </summary>
        private readonly List<FileSystemWatcher> modsWatchers = new List<FileSystemWatcher>();

        /// <summary>
        ///     looks for changes in packages folder
        /// </summary>
        private readonly List<FileSystemWatcher> packagesWatchers = new List<FileSystemWatcher>();

        private readonly IContentServiceClient service = GlobalConst.GetContentService();

        /// <summary>
        ///     queue of items to process
        /// </summary>
        private readonly LinkedList<WorkItem> workQueue = new LinkedList<WorkItem>();


        private CacheFile cache = new CacheFile();

        /// <summary>
        ///     Whether the cache need to be saved
        /// </summary>
        private bool isCacheDirty;

        private bool isDisposed;

        /// <summary>
        ///     number of work items being sent
        /// </summary>
        private int itemsSending;

        private DateTime lastCacheSave;

        /// <summary>
        ///     unitsync is run on this thread
        /// </summary>
        private Thread mainThread;
        private int workTotal;


        public SpringPaths SpringPaths { get; private set; }


        public bool WatchingEnabled
        {
            get { return mapsWatchers.First().EnableRaisingEvents; }
            set
            {
                foreach (var watcher in mapsWatchers.Concat(modsWatchers).Concat(packagesWatchers)) watcher.EnableRaisingEvents = value;
            }
        }

        public PlasmaResourceChecker(SpringPaths springPaths)
        {
            SpringPaths = springPaths;

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

        public void Dispose()
        {
            WatchingEnabled = false;
            isDisposed = true;
            if (isCacheDirty) SaveCache();
            GC.SuppressFinalize(this);
        }


        public bool HasResource(string name)
        {
            // scanner active
            return cache.NameIndex.ContainsKey(name);
        }


        public int GetWorkCost()
        {
            lock (workQueue)
            {
                return workQueue.Count;
            }
        }

        public void InitialScan()
        {
            CacheFile loadedCache = null;
            if (File.Exists(cachePath))
                try
                {
                    loadedCache = JsonConvert.DeserializeObject<CacheFile>(File.ReadAllText(cachePath));
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Warning: problem reading scanner cache: {0}", ex);
                    loadedCache = null;
                }

            if (loadedCache != null) cache = loadedCache;

            Rescan();

            Trace.TraceInformation("Initial scan done");
        }

        public event EventHandler<ResourceChangedEventArgs> LocalResourceAdded = delegate { };
        public event EventHandler<ResourceChangedEventArgs> LocalResourceRemoved = delegate { };
        
        public void Rescan()
        {
            var foundFiles = new Dictionary<string, bool>();

            InitialFolderScan("games", foundFiles);
            InitialFolderScan("maps", foundFiles);
            InitialFolderScan("packages", foundFiles);

            Dictionary<string, CacheItem> copy;
            lock (cache)
            {
                copy = new Dictionary<string, CacheItem>(cache.ShortPathIndex);
            }
            foreach (var pair in copy) if (!foundFiles.ContainsKey(pair.Key)) CacheItemRemove(pair.Value);
        }

        public event EventHandler<CancelEventArgs<CacheItem>> RetryResourceCheck = delegate { };
        // raised before attempting to reconnect to server to check for resource info

        public void Start(bool watchingEnabled = true)
        {
            WatchingEnabled = watchingEnabled;
            mainThread = Utils.SafeThread(MainThreadFunction);
            mainThread.Priority = ThreadPriority.BelowNormal;
            mainThread.Start();
        }

        // raised before attempting to upload unitsync data
        public event EventHandler<ProgressEventArgs> WorkProgressChanged = delegate { };
        public event EventHandler<ProgressEventArgs> WorkStarted = delegate { };
        public event EventHandler WorkStopped = delegate { };


        private void AddWork(string folder, string file, WorkItem.OperationType operationType, DateTime executeOn, bool toFront)
        {
            AddWork(new CacheItem { Folder = folder, FileName = file }, operationType, executeOn, toFront);
        }


        private void AddWork(CacheItem item, WorkItem.OperationType operationType, DateTime executeOn, bool toFront)
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


        private void CacheItemAdd(CacheItem item)
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

        private void CacheItemRemove(CacheItem item)
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

        private string GetFullPath(WorkItem work)
        {
            string fullPath = null;
            foreach (var directory in SpringPaths.DataDirectories)
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


        private WorkItem GetNextWorkItem()
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

        private void GetResourceData(WorkItem work)
        {
            ResourceData result = null;
            try
            {
                result = service.Query(new GetResourceDataRequest() {Md5 = work.CacheItem.Md5.ToString(), InternalName = work.CacheItem.InternalName});
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
                Trace.WriteLine(string.Format("No server resource data for {0}, asking later", work.CacheItem.ShortPath));
                AddWork(work.CacheItem, WorkItem.OperationType.ReAskServer, DateTime.Now.AddSeconds(UnitsyncMissingReaskQuery), false);
                return;
            }
            work.CacheItem.InternalName = result.InternalName;
            work.CacheItem.ResourceType = result.ResourceType;
            Trace.WriteLine(string.Format("Adding {0}", work.CacheItem.InternalName));
            CacheItemAdd(work.CacheItem);
        }

        private static string GetShortPath(string folder, string file)
        {
            return string.Format("{0}/{1}", folder, Path.GetFileName(file));
        }

        private string GetWatcherFolder(FileSystemWatcher watcher)
        {
            if (mapsWatchers.Contains(watcher)) return "maps";
            if (modsWatchers.Contains(watcher)) return "games";
            if (packagesWatchers.Contains(watcher)) return "packages";
            throw new ArgumentException("Invalid watcher", "watcher");
        }


        private void HandleWatcherChange(object sender, FileSystemEventArgs e)
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
                if (Utils.CanWrite(e.FullPath))
                {
                    // changed, created, renamed
                    // remove the item if present in the cache, then process the item
                    if (cache.ShortPathIndex.TryGetValue(shortPath, out item)) CacheItemRemove(item);
                    AddWork(folder, e.Name, WorkItem.OperationType.Hash, DateTime.Now, true);
                }
            }
        }


        private void InitialFolderScan(string folder, Dictionary<string, bool> foundFiles)
        {
            var fileList = new List<string>();
            foreach (var dd in SpringPaths.DataDirectories)
            {
                var path = Utils.MakePath(dd, folder);
                if (Directory.Exists(path))
                    try
                    {
                        fileList.AddRange(Directory.GetFiles(path));
                    }
                    catch { }
            }

            foreach (var f in fileList)
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


        private void MainThreadFunction()
        {
            try
            {
                InitialScan();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in scanner initial scan: {0}", ex);
            }

            var isWorking = false;
            var workDone = 0;
            while (!isDisposed)
                try
                {
                    Thread.Sleep(ScannerCycleTime);

                    if (isCacheDirty && (DateTime.Now.Subtract(lastCacheSave).TotalSeconds > DirtyCacheSave))
                    {
                        lastCacheSave = DateTime.Now;
                        isCacheDirty = false;
                        SaveCache();
                    }

                    WorkItem workItem;
                    while ((workItem = GetNextWorkItem()) != null)
                    {
                        if (isDisposed) return;

                        if (!isWorking)
                        {
                            isWorking = true;
                            workDone = 0;
                            workTotal = GetWorkCost();
                            WorkStarted(this, new ProgressEventArgs(workDone, workTotal, workItem.CacheItem.FileName));
                        }
                        else
                        {
                            workDone++;
                            workTotal = Math.Max(GetWorkCost(), workTotal);
                            WorkProgressChanged(this,
                                new ProgressEventArgs(workDone, workTotal, string.Format("{0} {1}", workItem.Operation, workItem.CacheItem.FileName)));
                        }

                        if (workItem.Operation == WorkItem.OperationType.Hash) PerformHashOperation(workItem);
                        if (workItem.Operation == WorkItem.OperationType.ReAskServer) GetResourceData(workItem);
                    }
                    if (isWorking)
                    {
                        isWorking = false;
                        WorkStopped(this, EventArgs.Empty);
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Exception in scanning thread: {0}", ex);
                }
        }


        private void PerformHashOperation(WorkItem work)
        {
            string fullPath = null;
            try
            {
                fullPath = GetFullPath(work);
                if (fullPath == null) throw new Exception("workitem file not found");

                using (var fs = File.OpenRead(fullPath))
                {
                    work.CacheItem.Md5 = Hash.HashStream(fs);
                }
                work.CacheItem.Length = (int)new FileInfo(fullPath).Length;

                if (!cache.HashIndex.ContainsKey(work.CacheItem.Md5)) GetResourceData(work);
            }
            catch (Exception e)
            {
                Trace.WriteLine("Can't hash " + work.CacheItem.ShortPath + " (" + e + ")");
            }
        }


        private void SaveCache()
        {
            lock (cache)
            {
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

        private void SetupWatcherEvents(IEnumerable<FileSystemWatcher> watchers)
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

        ~PlasmaResourceChecker()
        {
            Dispose();
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
        }


        public class ResourceChangedEventArgs : EventArgs
        {
            public CacheItem Item { get; protected set; }

            public ResourceChangedEventArgs(CacheItem item)
            {
                Item = item;
            }
        }


        [Serializable]
        private class CacheFile
        {
            public Dictionary<string, bool> FailedUnitSyncFiles = new Dictionary<string, bool>();
            public Dictionary<Hash, CacheItem> HashIndex = new Dictionary<Hash, CacheItem>();
            public Dictionary<string, CacheItem> NameIndex = new Dictionary<string, CacheItem>();
            public Dictionary<string, CacheItem> ShortPathIndex = new Dictionary<string, CacheItem>();
            public string SpringVersion;
        }


        private class WorkItem
        {
            public enum OperationType
            {
                Hash,
                ReAskServer,
                UnitSync
            }

            public readonly OperationType Operation;


            public CacheItem CacheItem;
            public DateTime ExecuteOn;


            public WorkItem(CacheItem item, OperationType operation, DateTime executeOn)
            {
                CacheItem = item;
                ExecuteOn = executeOn;
                Operation = operation;
            }
        }
    }

    public class ProgressEventArgs : EventArgs
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