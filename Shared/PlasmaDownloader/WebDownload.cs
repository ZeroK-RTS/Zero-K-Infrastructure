#region using

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Cache;
using PlasmaShared;

#endregion

namespace PlasmaDownloader
{
    /// <summary>
    /// Gets one file from web and returns resulting stream - notifies Program over progress in the process
    /// </summary>
    public class WebDownload : Download
    {
        WebClient wc;
        public byte[] Result;

        public WebDownload(string url)
        {
            Name = url;
        }

        public WebDownload() {}

        public void Start()
        {
            var newWebClient = new WebClient {Proxy = null};
            wc = newWebClient;
            newWebClient.DownloadProgressChanged += wc_ProgressChanged;
            newWebClient.DownloadDataCompleted += wc_DownloadDataCompleted;
            newWebClient.DownloadDataAsync(new Uri(Name), this);
        }

        public void Start(string s)
        {
            Name = s;
            Start();
        }

        public override void Abort()
        {
            wc.CancelAsync();
        }

        void wc_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            if (e.Error != null || e.Cancelled || e.Result == null) {
                Trace.TraceError("{0} failed {1}", Name, e.Error);
                Finish(false);
            } else {
                Result = e.Result;
                Trace.TraceInformation("{0} Completed - {1}", Name, Utils.PrintByteLength(Result.Length));
                Finish(true);
            }
        }


        void wc_ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Length = (int) e.TotalBytesToReceive;
            IndividualProgress = e.ProgressPercentage;
        }
    }
}