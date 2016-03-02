#region using

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using PlasmaDownloader.Packages;
using PlasmaDownloader.Torrents;
using ZkData;

#endregion

namespace PlasmaDownloader
{
    public enum DownloadType
    {
        MOD,
        MAP,
        MISSION,
        GAME,
        UNKNOWN,
        DEMO
    }


    public class PlasmaDownloader: IDisposable
    {
        private readonly List<Download> downloads = new List<Download>();

        private readonly PackageDownloader packageDownloader;
        private readonly SpringScanner scanner;
        private TorrentDownloader torrentDownloader;


        public IEnumerable<Download> Downloads {
            get { return downloads.AsReadOnly(); }
        }

        public PackageDownloader PackageDownloader {
            get { return packageDownloader; }
        }

        public SpringPaths SpringPaths { get; private set; }

        public event EventHandler<EventArgs<Download>> DownloadAdded = delegate { };

        public event EventHandler PackagesChanged {
            add { packageDownloader.PackagesChanged += value; }
            remove { packageDownloader.PackagesChanged -= value; }
        }

        public event EventHandler SelectedPackagesChanged {
            add { packageDownloader.SelectedPackagesChanged += value; }
            remove { packageDownloader.SelectedPackagesChanged -= value; }
        }

        public PlasmaDownloader(SpringScanner scanner, SpringPaths springPaths) {
            SpringPaths = springPaths;
            this.scanner = scanner;
            //torrentDownloader = new TorrentDownloader(this);
            packageDownloader = new PackageDownloader(this);
        }

        public void Dispose() {
            packageDownloader.Dispose();
        }

        /// <summary>
        /// Download requested Spring version, then call SetEnginePath() after finishes.
        /// Parameter "forSpringPaths" allow you to set a custom SpringPath for which to call SetEnginePath() 
        /// on behalf off (is useful for Autohost which run multiple Spring version but is sharing single downloader)
        /// </summary>
        public Download GetAndSwitchEngine(string version, SpringPaths forSpringPaths=null ) {
            if (forSpringPaths == null) 
                forSpringPaths = SpringPaths;
            lock (downloads) {
                downloads.RemoveAll(x => x.IsAborted || x.IsComplete != null); // remove already completed downloads from list}
                var existing = downloads.SingleOrDefault(x => x.Name == version);
                if (existing != null) return existing;

                if (SpringPaths.HasEngineVersion(version)) {
                    forSpringPaths.SetEnginePath(SpringPaths.GetEngineFolderByVersion(version));
                    return null;
                }
                else {
                    var down = new EngineDownload(version, forSpringPaths);
                    downloads.Add(down);
                    DownloadAdded.RaiseAsyncEvent(this, new EventArgs<Download>(down));
                    down.Start();
                    return down;
                }
            }
        }


        [CanBeNull]
        public Download GetResource(DownloadType type, string name) {

            lock (downloads) {
                downloads.RemoveAll(x => x.IsAborted || x.IsComplete != null); // remove already completed downloads from list}
                var existing = downloads.FirstOrDefault(x => x.Name == name || x.Alias == name);
                if (existing != null) return existing;
            }

            if (scanner != null && scanner.HasResource(name)) return null;
            
           
            lock (downloads) {

                if (type == DownloadType.DEMO) {
                    var target = new Uri(name);
                    var targetName = target.Segments.Last();
                    var filePath = Utils.MakePath(SpringPaths.WritableDirectory, "demos", targetName);
                    if (File.Exists(filePath)) return null;
                    var down = new WebFileDownload(name, filePath, null);
                    downloads.Add(down);
                    DownloadAdded.RaiseAsyncEvent(this, new EventArgs<Download>(down)); //create dowload bar (handled by MainWindow.cs)
                    down.Start();
                    return down;
                }

                if (type == DownloadType.MOD || type == DownloadType.UNKNOWN) {
                    var down = packageDownloader.GetPackageDownload(name);
                    if (down != null) {
                        down.Alias = name;
                        downloads.Add(down);
                        DownloadAdded.RaiseAsyncEvent(this, new EventArgs<Download>(down));
                        return down;
                    }
                }

                if (type == DownloadType.MAP || type == DownloadType.MOD || type == DownloadType.UNKNOWN || type == DownloadType.MISSION) {
                    if (torrentDownloader == null) torrentDownloader = new TorrentDownloader(this); //lazy initialization
                    var down = torrentDownloader.DownloadTorrent(name);
                    if (down != null) {
                        downloads.Add(down);
                        DownloadAdded.RaiseAsyncEvent(this, new EventArgs<Download>(down));
                        return down;
                    }
                }

                if (type == DownloadType.GAME) throw new ApplicationException(string.Format("{0} download not supported in this version", type));

                return null;
            }
        }

        public Download GetDependenciesOnly(string resourceName)
        {
            packageDownloader.LoadMasterAndVersions(false).Wait();
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
                        var dd = GetResource(DownloadType.UNKNOWN, dept);
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