#region using



#endregion

namespace PlasmaDownloader.Torrents
{
    public class TorrentDownload: Download
    {
        bool isAborting;
        internal string FileName;
        internal DownloadType TypeOfResource;

        internal TorrentDownload(string name)
        {
            Name = name;
        }

        public override void Abort()
        {
            IsAborted = true;
            foreach (var list in neededDownloads) list.Abort();
        }

        protected internal override void Finish(bool isComplete)
        {
            if (isAborting) IsAborted = true;
            base.Finish(isComplete);
        }
    }
}