using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using PlasmaShared;

namespace PlasmaDownloader
{
    internal class WebFileDownload : Download
    {
        #region Fields

        private string targetFilePath;
        private string tempFilePath;
        private string url;
        private WebClient wc;

        #endregion

        #region Constructors

        public WebFileDownload(string url, string targetFilePath, string tempFolder)
        {
            Name = url;
            this.url = url;
            this.targetFilePath = targetFilePath;
            if (tempFolder != null) tempFilePath = Utils.MakePath(tempFolder, Path.GetFileName(targetFilePath));
            else tempFilePath = Path.GetTempFileName();
        }

        #endregion

        #region Public methods

        public void Start()
        {
            var newWebClient = new WebClient {Proxy = null};
            wc = newWebClient;
            newWebClient.DownloadProgressChanged += wc_ProgressChanged;
            newWebClient.DownloadFileCompleted += wc_DownloadFileCompleted;
            newWebClient.DownloadFileAsync(new Uri(url), tempFilePath, this);
        }

        #endregion

        #region Overrides

        public override void Abort()
        {
            wc.CancelAsync();
        }

        #endregion

        #region Event Handlers

        private void wc_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null || e.Cancelled) {
                Trace.TraceError("{0} failed {1}", Name, e.Error);
                Finish(false);
            } else {
                Trace.TraceInformation("{0} Completed - {1}", Name, Utils.PrintByteLength(Length));
                try {
                    File.Delete(targetFilePath);
                } catch {}
                try {
                    File.Move(tempFilePath, targetFilePath);
                } catch {
                    Trace.TraceError("Error moving file from {0} to {0}", tempFilePath, targetFilePath);
                    Finish(false);
                    return;
                }
                Finish(true);
            }
        }


        private void wc_ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Length = (int) e.TotalBytesToReceive;
            IndividualProgress = e.ProgressPercentage;
        }

        #endregion
    }
}