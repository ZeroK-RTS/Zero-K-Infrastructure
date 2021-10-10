#region using

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using JetBrains.Annotations;
using PlasmaDownloader.Packages;
using PlasmaDownloader.Torrents;
using ZkData;

#endregion

namespace PlasmaDownloader
{
    public enum DownloadType
    {
        RAPID,
        MAP,
        MISSION,
        DEMO,
        ENGINE,
        NOTKNOWN
    }

    public enum RapidHandling
    {
        /// <summary>
        /// Default SDP download, keeps SDP
        /// </summary>
        DefaultSdp,
        /// <summary>
        /// SDZ named based on hash. Deletes other verisons of the same game
        /// </summary>
        SdzNameHash,
        /// <summary>
        /// SDZ with name based on tag. Existing version is not checked, forces re-download
        /// </summary>
        SdzNameTagForceDownload
    }

    public class PlasmaDownloader : IDisposable
    {
        private readonly ConcurrentDictionary<string, Download> downloads = new ConcurrentDictionary<string, Download>();

        private readonly PackageDownloader packageDownloader;
        private TorrentDownloader torrentDownloader;
        private IResourcePresenceChecker scanner;


        public RapidHandling RapidHandling = RapidHandling.DefaultSdp;

        /// <summary>
        /// Forces map download even if the map is presnet, workaround for VFS issue in chobby
        /// </summary>
        public bool ForceMapRedownload = false;


        public IReadOnlyCollection<Download> Downloads => new List<Download>(downloads.Values.Where(x=>x!= null)).AsReadOnly();

        public PackageDownloader PackageDownloader
        {
            get { return packageDownloader; }
        }

        public SpringPaths SpringPaths { get; private set; }

        public event EventHandler<EventArgs<Download>> DownloadAdded = delegate { };

        public event EventHandler PackagesChanged
        {
            add { packageDownloader.PackagesChanged += value; }
            remove { packageDownloader.PackagesChanged -= value; }
        }

        public PlasmaDownloader(IResourcePresenceChecker checker, SpringPaths paths)
        {
            SpringPaths = paths;
            this.scanner = checker;
            //torrentDownloader = new TorrentDownloader(this);
            packageDownloader = new PackageDownloader(this);

            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12; 
        }

        public void Dispose()
        {
            packageDownloader.Dispose();
        }

        private object locker = new Object();

        [CanBeNull]
        public Download GetResource(DownloadType type, string name)
        {
            if (name.StartsWith("rapid://")) // note this is not super clean as supplied name might be used for tracking.
            {
                name = name.Substring(8);
                type = DownloadType.RAPID;
            }
            
            if (name == "zk:dev" || name == "Zero-K $VERSION") return null;
            lock (locker)
            {
                // remove already completed downloads from list
                foreach (var d in downloads.Values.ToList())
                {
                    if (d != null && (d.IsAborted || d.IsComplete != null))
                    {
                        Download dummy;
                        downloads.TryRemove(d.Name, out dummy);
                    }
                }

                
                var existing = downloads.Values.FirstOrDefault(x => x!=null && (x.Name == name || x.Alias == name));
                if (existing != null) return existing;

                if (scanner?.HasResource(name) == true) return null;
                if (SpringPaths.HasEngineVersion(name)) return null;


                // check rapid to determine type
                if (type == DownloadType.NOTKNOWN)
                {
                    if (packageDownloader.GetByInternalName(name) != null || packageDownloader.GetByTag(name) != null) type = DownloadType.RAPID;
                    else
                    {
                        packageDownloader.LoadMasterAndVersions().Wait();
                        if (packageDownloader.GetByInternalName(name) != null || packageDownloader.GetByTag(name) != null) type = DownloadType.RAPID;
                        else type = DownloadType.MAP;
                    }
                }



                if (type == DownloadType.DEMO)
                {
                    var target = new Uri(name);
                    var targetName = target.Segments.Last();
                    var filePath = Utils.MakePath(SpringPaths.WritableDirectory, "demos", targetName);
                    if (File.Exists(filePath)) return null;
                    var down = new WebFileDownload(name, filePath, null);
                    down.DownloadType = type;
                    downloads[down.Name] = down;
                    DownloadAdded.RaiseAsyncEvent(this, new EventArgs<Download>(down)); //create download bar (handled by MainWindow.cs)
                    down.Start();
                    return down;
                }

                
                if (type == DownloadType.MAP || type == DownloadType.MISSION)
                {
                    if (torrentDownloader == null) torrentDownloader = new TorrentDownloader(this); //lazy initialization
                    var down = torrentDownloader.DownloadTorrent(name);
                    if (down != null)
                    {
                        down.DownloadType = type;
                        downloads[down.Name] = down;
                        DownloadAdded.RaiseAsyncEvent(this, new EventArgs<Download>(down));
                        return down;
                    }
                }

                if (type == DownloadType.RAPID)
                {
                    var down = packageDownloader.GetPackageDownload(name);
                    if (down != null)
                    {
                        down.DownloadType = type;
                        down.Alias = name;
                        downloads[down.Name] = down;
                        DownloadAdded.RaiseAsyncEvent(this, new EventArgs<Download>(down));
                        return down;
                    }
                }

                if (type == DownloadType.ENGINE)
                {
                    var down = new EngineDownload(name, SpringPaths);
                    down.DownloadType = type;
                    downloads[down.Name] = down;
                    DownloadAdded.RaiseAsyncEvent(this, new EventArgs<Download>(down));
                    down.Start();
                    return down;
                }

                return null;
            }
        }

        public Download GetDependenciesOnly(string resourceName)
        {
            packageDownloader.LoadMasterAndVersions()?.Wait();
            var dep = packageDownloader.GetPackageDependencies(resourceName);
            if (dep == null)
            {
                if (torrentDownloader == null)
                    torrentDownloader = new TorrentDownloader(this); //lazy initialization
                dep = torrentDownloader.GetFileDependencies(resourceName);
            }
            if (dep != null)
            {
                Download down = null;
                foreach (var dept in dep)
                {
                    if (!string.IsNullOrEmpty(dept))
                    {
                        var dd = GetResource(DownloadType.NOTKNOWN, dept);
                        if (dd != null)
                        {
                            if (down == null) down = dd;
                            else down.AddNeededDownload(dd);
                        }
                    }
                }
                return down;
            }
            return null;
        }

    }
}