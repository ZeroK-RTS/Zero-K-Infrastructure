#region using

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MonoTorrent.Common;
using PlasmaShared;
using ZkData;

#endregion

namespace PlasmaDownloader.Torrents
{
    public class TorrentDownloader
    {
        readonly string incomingFolder;
        PlasmaDownloader plasmaDownloader;
        readonly IContentService plasmaService = GlobalConst.GetContentService();

        public TorrentDownloader(PlasmaDownloader plasmaDownloader)
        {
            this.plasmaDownloader = plasmaDownloader;
            Utils.CheckPath(Utils.MakePath(this.plasmaDownloader.SpringPaths.WritableDirectory, "maps"));
            Utils.CheckPath(Utils.MakePath(this.plasmaDownloader.SpringPaths.WritableDirectory, "games"));
            incomingFolder = Utils.MakePath(this.plasmaDownloader.SpringPaths.Cache, "Incoming");
            Utils.CheckPath(incomingFolder);

        }

        private static Torrent CreateTorrentFromFile(string path)
        {
            try
            {
                var creator = new TorrentCreator();
                creator.Path = path;
                var ms = new MemoryStream();
                creator.Create(ms);
                ms.Seek(0, SeekOrigin.Begin);
                return Torrent.Load(ms);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error creating torrent from file {0}", path);
                return null;
            }
        }


        public TorrentDownload DownloadTorrent(string name)
        {
            Trace.TraceInformation("starting download {0}", name);
            var down = new TorrentDownload(name);


            Task.Factory.StartNew(() => {
                DownloadFileResponse e;
                try {
                    e = plasmaService.DownloadFile(name);
                } catch (Exception ex) {
                    Trace.TraceError("Error downloading {0}: {1}", down.Name, ex);
                    down.Finish(false);
                    return;
                }

                if (e == null || e.links == null || e.torrent == null || e.links.Count == 0)
                {
                    Trace.TraceWarning("Cannot download {0}, not registered or has no links", down.Name);
                    down.Finish(false);
                    return;
                }

                try
                {
                    var tor = Torrent.Load(e.torrent);
                    down.FileName = tor.Files[0].Path;
                    //down.Length = (int)tor.Size;
                    down.Length = 1;//workaround for progress bars
                    down.TypeOfResource = e.resourceType == ResourceType.Map ? DownloadType.MAP : DownloadType.RAPID;


                    foreach (var dependency in e.dependencies)
                    {
                        var dep = plasmaDownloader.GetResource(DownloadType.NOTKNOWN, dependency);
                        if (dep != null) down.AddNeededDownload(dep);
                    }


                    if (!plasmaDownloader.ForceMapRedownload)
                    {
                        var defPath = Utils.MakePath(plasmaDownloader.SpringPaths.WritableDirectory,
                            down.TypeOfResource == DownloadType.MAP ? "maps" : "games",
                            down.FileName);

                        if (File.Exists(defPath))
                        {
                            var exTor = CreateTorrentFromFile(defPath);
                            if (exTor != null && exTor.InfoHash.Equals(tor.InfoHash))
                            {
                                down.Finish(true);
                                return; // done
                            }
                        }
                    }

                    var wd = new WebMultiDownload(e.links.Shuffle(), GetDestPath(down.TypeOfResource, down.FileName), incomingFolder, tor);
                    down.AddNeededDownload(wd);
                    down.Finish(true); // mark current torrent dl as complete - will wait for dependency
                    wd.Start(); // start dependent download

                }
                catch (Exception ex)
                {
                    Trace.TraceError("downloading torrent failed {0}", ex);
                    down.Finish(false);
                }

            });
            
            
            return down;
        }

        public string[] GetFileDependencies(string name)
        {
            DownloadFileResponse e;
            try {
                e = plasmaService.DownloadFile(name);
                return e.dependencies.ToArray();
            } catch (Exception ex) {
                Trace.TraceError("Error fetching information for {0}: {1}", name, ex);
                return null;
            }
        }

        string GetDestPath(DownloadType type, string fileName)
        {
            return
                Utils.GetAlternativeFileName(Utils.MakePath(plasmaDownloader.SpringPaths.WritableDirectory , type== DownloadType.MAP?"maps":"games",fileName));
        }

      
    }
}