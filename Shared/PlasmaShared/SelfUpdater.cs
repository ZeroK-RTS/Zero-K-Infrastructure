using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using Mono.Unix.Native;
using Newtonsoft.Json;


namespace ZkData
{
    public class SelfChecker
    {
        readonly string urlBase;
        readonly string urlUpdateName;
        public string CurrentVersion { get; private set; }
        public string LatestVersion { get; private set; }

        public SelfChecker(string updateName = null, string urlBase = null)
        {
            this.urlBase = urlBase;
            if (string.IsNullOrEmpty(this.urlBase)) this.urlBase = GlobalConst.SelfUpdaterBaseUrl;
            var entry = Assembly.GetEntryAssembly();
            CurrentVersion = entry.GetName().Version.ToString();
            urlUpdateName = updateName;
        }


        public bool CheckForUpdate()
        {
            LatestVersion = GetLatestVersion();
            if (!string.IsNullOrEmpty(LatestVersion) && LatestVersion != CurrentVersion) {
                Trace.TraceInformation("{0} updated to {1}", urlUpdateName, LatestVersion);
                return true;
            } else return false; // no update
        }


        string GetLatestVersion()
        {
            var wc = new WebClient { Proxy = null };
            try
            {
                return wc.DownloadString(string.Format("{0}/{1}.version.txt", urlBase, urlUpdateName)).Trim();
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error getting new version number: {0}", ex.Message);
                return null;
            }
        }

    }
}