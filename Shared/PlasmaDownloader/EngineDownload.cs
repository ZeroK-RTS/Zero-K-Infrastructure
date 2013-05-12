using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using PlasmaShared;
using SharpCompress.Archive;
using SharpCompress.Reader;

namespace PlasmaDownloader
{
    public class EngineDownload: Download
    {
        const string EngineDownloadPath = "http://springrts.com/dl/";
        readonly SpringPaths springPaths;
        WebClient wc;


        public EngineDownload(string version, SpringPaths springPaths)
        {
            this.springPaths = springPaths;
            Name = version;
        }

        public void Start()
        {
            Utils.StartAsync(() =>
                {
                    var paths = new List<string>();

                    paths.Add(string.Format("{0}buildbot/default/master/{1}/spring_{1}_minimal-portable+dedicated.zip",
                                            EngineDownloadPath,
                                            Name));
                    paths.Add(string.Format("{0}buildbot/default/develop/{1}/spring_{{develop}}{1}_minimal-portable+dedicated.zip",
                                            EngineDownloadPath,
                                            Name));
                    paths.Add(string.Format("{0}buildbot/default/release/{1}/spring_{{release}}{1}_minimal-portable+dedicated.zip",
                                            EngineDownloadPath,
                                            Name));
                    paths.Add(string.Format("{0}buildbot/default/MTsim/{1}/spring_{{MTsim}}{1}_minimal-portable+dedicated.zip",
                                            EngineDownloadPath,
                                            Name));
					paths.Add(string.Format("{0}buildbot/default/master/{1}/win32/spring_{1}_minimal-portable+dedicated.zip",
                                            EngineDownloadPath,
                                            Name));
                    paths.Add(string.Format("{0}buildbot/default/develop/{1}/win32/spring_{{develop}}{1}_minimal-portable+dedicated.zip",
                                            EngineDownloadPath,
                                            Name));
                    paths.Add(string.Format("{0}buildbot/default/release/{1}/win32/spring_{{release}}{1}_minimal-portable+dedicated.zip",
                                            EngineDownloadPath,
                                            Name));
                    paths.Add(string.Format("{0}buildbot/default/MTsim/{1}/win32/spring_{{MTsim}}{1}_minimal-portable+dedicated.zip",
                                            EngineDownloadPath,
                                            Name));

                    for (var i = 9; i >= -1; i--)
                    {
                        var version = Name;
                        // if i==-1 we tested without version number
                        if (i >= 0) version = string.Format("{0}.{1}", Name, i);
                        paths.Add(string.Format("{0}spring_{1}.zip", EngineDownloadPath, version));
                    }

                    for (var i = 9; i >= -1; i--)
                    {
                        var version = Name;
                        // if i==-1 we tested without version number
                        if (i >= 0) version = string.Format("{0}.{1}", Name, i);
                        paths.Add(string.Format("{0}buildbot/default/master/{1}/spring_{1}_minimal-portable+dedicated.zip",
                                            EngineDownloadPath,
                                            version));
                    }

                    var source = paths.FirstOrDefault(VerifyFile);
                    var extension = ".zip";

                    if (source != null)
                    {
                        var wc = new WebClient() { Proxy = null };
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
                                    Trace.TraceInformation("Installing {0}", source);
                                    var timer = new Timer((o) => { IndividualProgress += (100 - IndividualProgress)/10; }, null, 1000, 1000);

                                    if (extension == ".exe")
                                    {
                                        var p = new Process();
                                        p.StartInfo = new ProcessStartInfo(target,
                                                                           string.Format("/S /D={0}", springPaths.GetEngineFolderByVersion(Name)));
                                        p.Exited += (s2, e2) =>
                                            {
                                                timer.Dispose();
                                                if (p.ExitCode != 0)
                                                {
                                                    Trace.TraceWarning("Install of {0} failed: {1}", Name, p.ExitCode);
                                                    Finish(false);
                                                }
                                                else
                                                {
                                                    Trace.TraceInformation("Install of {0} complete", Name);
                                                    springPaths.SetEnginePath(springPaths.GetEngineFolderByVersion(Name));
                                                    Finish(true);
                                                }
                                            };

                                        p.EnableRaisingEvents = true;
                                        p.Start();
                                    }
                                    else
                                    {
                                        using (var archive = ArchiveFactory.Open(target)) {
                                            long done = 0;
                                            long totalSize = archive.TotalSize;
                                            archive.EntryExtractionEnd += (sender, args) =>
                                                {
                                                    done += args.Item.CompressedSize;
                                                    if (totalSize > 0) IndividualProgress = 90 + (10*done/totalSize);
                                                };
                                            var targetDir = springPaths.GetEngineFolderByVersion(Name);
                                            try {
                                                
                                                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                                                foreach (var entry in archive.Entries.Where(x=>x.IsDirectory)) {
                                                    Directory.CreateDirectory(Path.Combine(targetDir, entry.FilePath));
                                                }

                                                foreach (var entry in archive.Entries.Where(x => !x.IsDirectory)) {
                                                    entry.WriteToFile(Path.Combine(targetDir, entry.FilePath));
                                                }

                                                Trace.TraceInformation("Install of {0} complete", Name);
                                                springPaths.SetEnginePath(targetDir);
                                                Finish(true);
                                            }
                                            catch (Exception ex)
                                            {
                                                try {
                                                    Directory.Delete(targetDir, true);
                                                } catch {}
                                                Trace.TraceWarning("Install of {0} failed: {1}", Name, ex);
                                                Finish(false);
                                            }
                                        }
                                    }
                                }
                            };
                        Trace.TraceInformation("Downloading {0}", source);
                        wc.DownloadFileAsync(new Uri(source), target, this);
                        return;
                    }
                    Trace.TraceInformation("Cannot find {0}", Name);
                    Finish(false);
                });
        }


        static bool VerifyFile(string url)
        {
            try
            {
                var request = WebRequest.Create(url);
                request.Method = "HEAD";
                request.Timeout = 4000;
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
    }
}