#region using

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using PlasmaDownloader.Packages;
using PlasmaDownloader.Torrents;
using PlasmaShared;

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

        private PackageDownloader packageDownloader;
        private readonly SpringScanner scanner;
        private TorrentDownloader torrentDownloader;

        public IPlasmaDownloaderConfig Config { get; private set; }

        public IEnumerable<Download> Downloads {
            get { return downloads.AsReadOnly(); }
        }

        public PackageDownloader PackageDownloader {
            get {
                InitializePackageDownloader();
                return packageDownloader; 
            }
        }

        public SpringPaths SpringPaths { get; private set; }

        public event EventHandler<EventArgs<Download>> DownloadAdded = delegate { };

        public event EventHandler PackagesChanged {
            add {
                InitializePackageDownloader();
                packageDownloader.PackagesChanged += value; }
            remove {
                if (packageDownloader != null)
                    packageDownloader.PackagesChanged -= value; }
        }

        public event EventHandler SelectedPackagesChanged {
            add {
                InitializePackageDownloader();
                packageDownloader.SelectedPackagesChanged += value; }
            remove {
                if (packageDownloader!=null)
                    packageDownloader.SelectedPackagesChanged -= value; }
        }

        public PlasmaDownloader(IPlasmaDownloaderConfig config, SpringScanner scanner, SpringPaths springPaths) {
            SpringPaths = springPaths;
            Config = config;
            this.scanner = scanner;
            //torrentDownloader = new TorrentDownloader(this);
            //packageDownloader = new PackageDownloader(this);
        }

        public void Dispose() {
            if (packageDownloader!=null) 
                packageDownloader.Dispose();
        }

        private void InitializePackageDownloader() //for lazy initialization (initialize when needed)
        {
            if (packageDownloader == null) 
                packageDownloader = new PackageDownloader(this);
        }

        private void InitializeTorrentDownloader() //for lazy initialization
        {
            if (torrentDownloader == null) 
                torrentDownloader = new TorrentDownloader(this);
        }


        public Download GetAndSwitchEngine(string version) {
            lock (downloads) {
                downloads.RemoveAll(x => x.IsAborted || x.IsComplete != null); // remove already completed downloads from list}
                var existing = downloads.SingleOrDefault(x => x.Name == version);
                if (existing != null) return existing;

                if (SpringPaths.HasEngineVersion(version)) {
                    SpringPaths.SetEnginePath(SpringPaths.GetEngineFolderByVersion(version));
                    return null;
                }
                else {
                    var down = new EngineDownload(version, SpringPaths);
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
                var existing = downloads.SingleOrDefault(x => x.Name == name);
                if (existing != null) return existing;

                if (scanner != null && scanner.HasResource(name)) return null;

                if (type == DownloadType.DEMO) {
                    var target = new Uri(name);
                    var targetName = target.Segments.Last();
                    var filePath = Utils.MakePath(SpringPaths.WritableDirectory, "demos", targetName);
                    if (File.Exists(filePath)) return null;
                    try {
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    } catch {}
                    var down = new WebFileDownload(name, filePath, null);
                    downloads.Add(down);
                    DownloadAdded.RaiseAsyncEvent(this, new EventArgs<Download>(down)); //create dowload bar (handled by MainWindow.cs)
                    down.Start();
                    return down;
                }

                if (type == DownloadType.MOD || type == DownloadType.UNKNOWN) {
                    InitializePackageDownloader();
                    var down = packageDownloader.GetPackageDownload(name);
                    if (down != null) {
                        downloads.Add(down);
                        DownloadAdded.RaiseAsyncEvent(this, new EventArgs<Download>(down));
                        return down;
                    }
                }

                if (type == DownloadType.MAP || type == DownloadType.MOD || type == DownloadType.UNKNOWN || type == DownloadType.MISSION) {
                    InitializeTorrentDownloader();
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
    }
}