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
        UNKNOWN,
        DEMO,
        ENGINE
    }


    public class PlasmaDownloader : IDisposable
    {
        private readonly List<Download> downloads = new List<Download>();

        private readonly PackageDownloader packageDownloader;
        private readonly SpringScanner scanner;
        private TorrentDownloader torrentDownloader;


        public IEnumerable<Download> Downloads
        {
            get { return downloads.AsReadOnly(); }
        }

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

        public PlasmaDownloader(SpringScanner scanner, SpringPaths springPaths)
        {
            SpringPaths = springPaths;
            this.scanner = scanner;
            //torrentDownloader = new TorrentDownloader(this);
            packageDownloader = new PackageDownloader(this);
        }

        public void Dispose()
        {
            packageDownloader.Dispose();
        }


        [CanBeNull]
        public Download GetResource(DownloadType type, string name)
        {
            if (name == "zk:dev" || name == "Zero-K $VERSION") return null;
            lock (downloads)
            {
                downloads.RemoveAll(x => x.IsAborted || x.IsComplete != null); // remove already completed downloads from list}
                var existing = downloads.FirstOrDefault(x => x.Name == name || x.Alias == name);
                if (existing != null) return existing;
            }

            if (scanner != null)
            {
                if (scanner.HasResource(name)) return null;
                var tagged = PackageDownloader.GetByTag(name);
                if (tagged != null && scanner.HasResource(tagged.InternalName)) return null; // has it (referenced by tag)
            }
            if (SpringPaths.HasEngineVersion(name)) return null;


            lock (downloads)
            {

                if (type == DownloadType.DEMO)
                {
                    var target = new Uri(name);
                    var targetName = target.Segments.Last();
                    var filePath = Utils.MakePath(SpringPaths.WritableDirectory, "demos", targetName);
                    if (File.Exists(filePath)) return null;
                    var down = new WebFileDownload(name, filePath, null);
                    down.DownloadType = type;
                    downloads.Add(down);
                    DownloadAdded.RaiseAsyncEvent(this, new EventArgs<Download>(down)); //create dowload bar (handled by MainWindow.cs)
                    down.Start();
                    return down;
                }


                if (type == DownloadType.MAP || type == DownloadType.UNKNOWN || type == DownloadType.MISSION)
                {
                    if (torrentDownloader == null) torrentDownloader = new TorrentDownloader(this); //lazy initialization
                    var down = torrentDownloader.DownloadTorrent(name);
                    if (down != null)
                    {
                        down.DownloadType = type;
                        downloads.Add(down);
                        DownloadAdded.RaiseAsyncEvent(this, new EventArgs<Download>(down));
                        return down;
                    }
                }

                if (type == DownloadType.MOD || type == DownloadType.UNKNOWN)
                {
                    var down = packageDownloader.GetPackageDownload(name);
                    if (down != null)
                    {
                        down.DownloadType = type;
                        down.Alias = name;
                        downloads.Add(down);
                        DownloadAdded.RaiseAsyncEvent(this, new EventArgs<Download>(down));
                        return down;
                    }
                }


                if (type == DownloadType.ENGINE)
                {
                    var down = new EngineDownload(name, SpringPaths);
                    down.DownloadType = type;
                    downloads.Add(down);
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