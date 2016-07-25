using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using SharpCompress.Archive;
using SharpCompress.Common;
using ZkData;

namespace PlasmaDownloader
{
    public class EngineDownload : Download
    {
        readonly SpringPaths springPaths;


        public EngineDownload(string version, SpringPaths springPaths)
        {
            this.springPaths = springPaths;
            Name = version;
        }

        public static List<string> GetEngineList()
        {
            var srv = GlobalConst.GetContentService();
            return srv.GetEngineList(null);
        }

        public void Start()
        {
            Utils.StartAsync(() =>
                {
                    var platform = "win32";

                    if (Environment.OSVersion.Platform == PlatformID.Unix)
                    {
                        var response = Utils.ExecuteConsoleCommand("uname", "-m") ?? "";
                        platform = response.Contains("64") ? "linux64" : "linux32";
                    }

                    var downloadUrl = string.Format("{0}/engine/{2}/{1}.zip", GlobalConst.BaseSiteUrl, Name, platform);

                    if (VerifyFile(downloadUrl))
                    {
                        var extension = downloadUrl.Substring(downloadUrl.LastIndexOf('.'));
                        var wc = new WebClient() { Proxy = null };
                        var assemblyName = Assembly.GetEntryAssembly()?.GetName();
                        if (assemblyName != null) wc.Headers.Add("user-agent", string.Format("{0} {1}", assemblyName.Name, assemblyName.Version));
                        var target = Path.GetTempFileName() + extension;
                        wc.DownloadProgressChanged += (s, e) =>
                        {
                            Length = (int)(e.TotalBytesToReceive);
                            IndividualProgress = 10 + 0.8*e.ProgressPercentage;
                        };
                        wc.DownloadFileCompleted += (s, e) =>
                        {
                            if (e.Cancelled)
                            {
                                Trace.TraceInformation("Download {0} cancelled", Name);
                                Finish(false);
                            }
                            else if (e.Error != null)
                            {
                                Trace.TraceWarning("Error downloading {0}: {1}", Name, e.Error);
                                Finish(false);
                            }
                            else
                            {
                                Trace.TraceInformation("Installing {0}", downloadUrl);
                                var timer = new Timer((o) => { IndividualProgress += (100 - IndividualProgress)/10; }, null, 1000, 1000);
                                
                                var targetDir = springPaths.GetEngineFolderByVersion(Name);
                                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                                try
                                {
                                    ExtractZipArchive(target, targetDir);
                                    Trace.TraceInformation("Install of {0} complete", Name);
                                    springPaths.SetEnginePath(targetDir);
                                    Finish(true);
                                }
                                catch (Exception ex)
                                {
                                    try
                                    {
                                        Directory.Delete(targetDir, true);
                                    }
                                    catch {}
                                    Trace.TraceWarning("Install of {0} failed: {1}", Name, ex);
                                    Finish(false);
                                }
                                finally
                                {
                                    timer.Dispose();
                                }
                            }
                        };
                        Trace.TraceInformation("Downloading {0}", downloadUrl);
                        wc.DownloadFileAsync(new Uri(downloadUrl), target, this);
                        return;
                    }
                    else
                    {
                        Trace.TraceInformation("Cannot find {0}", Name);
                        Finish(false);
                    }
                });
        }

        void ExtractZipArchive(string target, string targetDir)
        {
            using (var archive = ArchiveFactory.Open(target))
            {
                long done = 0;
                var totalSize = archive.Entries.Count() + 1;
                archive.EntryExtractionEnd += (sender, args) =>
                    {
                        done++;
                        IndividualProgress = 90 + (10 * done / totalSize);
                    };

                archive.WriteToDirectory(targetDir, ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);
            }
        }


        static bool VerifyFile(string url)
        {
            try
            {
                var request = WebRequest.Create(url);
                request.Method = "HEAD";
                request.Timeout = 5000;
                var res = request.GetResponse();
                var len = res.ContentLength;
                request.Abort();
                return len > 100000;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public class VersionNumberComparer : IComparer<string>
        {
            public int Compare(string a, string b)
            {
                var pa = a.Split(new char[] { '.', '-' });
                var pb = b.Split(new char[] { '.', '-' });

                for (var i = 0; i < Math.Min(pa.Length, pb.Length); i++)
                {
                    int va;
                    int vb;
                    if (int.TryParse(pa[i], out va) && int.TryParse(pb[i], out vb) && va != vb) return va.CompareTo(vb);
                    else if (pa[i] != pb[i]) return String.Compare(pa[i], pb[i], StringComparison.Ordinal);
                }
                return pa.Length.CompareTo(pb.Length);
            }
        }

    }
}

