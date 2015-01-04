#region using

using System;
using System.Diagnostics;
using System.Linq;
using MonoTorrent.Common;
using PlasmaShared.ContentService;
using ZkData;

#endregion

namespace PlasmaDownloader.Torrents
{
    public class TorrentDownloader
    {
        readonly string incomingFolder;
        PlasmaDownloader plasmaDownloader;
        readonly ContentService plasmaService = new ContentService() { Proxy = null };

        public TorrentDownloader(PlasmaDownloader plasmaDownloader)
        {
            this.plasmaDownloader = plasmaDownloader;
            Utils.CheckPath(Utils.MakePath(this.plasmaDownloader.SpringPaths.WritableDirectory, "maps"));
            Utils.CheckPath(Utils.MakePath(this.plasmaDownloader.SpringPaths.WritableDirectory, "games"));
            incomingFolder = Utils.MakePath(this.plasmaDownloader.SpringPaths.Cache, "Incoming");
            Utils.CheckPath(incomingFolder);

            plasmaService.DownloadFileCompleted += HandlePlasmaServiceDownloadFileCompleted;
        }


        public TorrentDownload DownloadTorrent(string name)
        {
            Trace.TraceInformation("starting download {0}", name);
            var down = new TorrentDownload(name);
            plasmaService.DownloadFileAsync(name, down);
            return down;
        }


        string GetDestPath(DownloadType type, string fileName)
        {
            return
                Utils.GetAlternativeFileName(Utils.MakePath(plasmaDownloader.SpringPaths.WritableDirectory,
                                                            type == DownloadType.MAP ? "maps" : "games",
                                                            fileName));
        }

        void HandlePlasmaServiceDownloadFileCompleted(object sender, DownloadFileCompletedEventArgs e)
        {
            var down = (TorrentDownload)e.UserState;
            if (e.Error != null || e.Cancelled)
            {
                Trace.TraceError("Error downloading {0}: {1}", down.Name, e.Error);
                down.Finish(false);
                return;
            }

            if (!e.Result || e.links == null || e.torrent == null || e.links.Count() == 0)
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
                down.TypeOfResource = e.resourceType == ResourceType.Map ? DownloadType.MAP : DownloadType.MOD;


                // just 1 link, use normal webdownload
                if (e.links.Count() == 1 || e.links.Count(x=>!x.Contains("springfiles.com"))== 1) // mirrors or mirros without jobjol = 1
                {
                    var wd = new WebFileDownload(e.links[0], GetDestPath(down.TypeOfResource, down.FileName), incomingFolder);
                    down.AddNeededDownload(wd);
                    down.Finish(true); // mark current torrent dl as complete - will wait for dependency
                    wd.Start(); // start dependent download
                }
                else
                {
                    var wd = new WebMultiDownload(e.links.Shuffle(), GetDestPath(down.TypeOfResource, down.FileName), incomingFolder, tor);
                    down.AddNeededDownload(wd);
                    down.Finish(true); // mark current torrent dl as complete - will wait for dependency
                    wd.Start(); // start dependent download
                }
                foreach (var dependency in e.dependencies)
                {
                    var dep = plasmaDownloader.GetResource(DownloadType.UNKNOWN, dependency);
                    if (dep != null) down.AddNeededDownload(dep);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("downloading torrent failed {0}", ex);
                down.Finish(false);
            }
        }
    }
}