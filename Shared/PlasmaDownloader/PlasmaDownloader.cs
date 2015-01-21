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

        public IPlasmaDownloaderConfig Config { get; private set; }

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

        public PlasmaDownloader(IPlasmaDownloaderConfig config, SpringScanner scanner, SpringPaths springPaths) {
            SpringPaths = springPaths;
            Config = config;
            this.scanner = scanner;
            //torrentDownloader = new TorrentDownloader(this);
            packageDownloader = new PackageDownloader(this);
        }

        public void Dispose() {
            packageDownloader.Dispose();
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
                var existing = downloads.FirstOrDefault(x => x.Name == name);
                if (existing != null) return existing;
            }

            if (type == DownloadType.MOD || type == DownloadType.UNKNOWN)
            {
                packageDownloader.LoadMasterAndVersions(false).Wait();
            }
            
            lock (downloads) {

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
                    var down = packageDownloader.GetPackageDownload(name);
                    if (down != null) {
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
    }
}