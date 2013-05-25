using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using PlasmaShared;
using SharpCompress.Archive;
using SharpCompress.Common;

namespace PlasmaDownloader
{
    public class EngineDownload: Download
    {
        const string EngineDownloadPath = "http://springrts.com/dl/";
        readonly SpringPaths springPaths;
        WebClient wc;


        public EngineDownload(string version, SpringPaths springPaths) {
            this.springPaths = springPaths;
            Name = version;
        }

        public static List<string> GetDevelopEngineList() {
            var data = new WebClient().DownloadString(string.Format("{0}buildbot/default/develop/", EngineDownloadPath));
            return Regex.Matches(data, "<img src=\"/icons/folder.gif\" alt=\"\\[DIR\\]\"></td><td><a href=\"([^\"]+)/\">\\1/</a>", RegexOptions.IgnoreCase)
                 .OfType<Match>()
                 .Select(x => x.Groups[1].Value).ToList();
        }

        public void Start() {
            Utils.StartAsync(() =>
                {
                    var paths = new List<string>();
                    var platform = "win32";
                    var archiveName = "minimal-portable+dedicated.zip";

                    if (Environment.OSVersion.Platform == PlatformID.Unix) {
                        var pi = new ProcessStartInfo("uname", "-m")
                        {
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                        };
                        var p = Process.Start(pi);
                        p.Start();
                        var response = p.StandardOutput.ReadToEnd();
                        p.WaitForExit();
                        platform = response.Contains("64") ? "linux64" : "linux32";
                        archiveName = string.Format("minimal-portable-{0}-static.7z", platform);
                    }

                    paths.Add(string.Format("{0}buildbot/default/master/{1}/spring_{1}_{2}", EngineDownloadPath, Name, archiveName));
                    paths.Add(string.Format("{0}buildbot/default/develop/{1}/spring_{{develop}}{1}_{2}", EngineDownloadPath, Name, archiveName));
                    paths.Add(string.Format("{0}buildbot/default/release/{1}/spring_{{release}}{1}_{2}", EngineDownloadPath, Name, archiveName));
                    paths.Add(string.Format("{0}buildbot/default/MTsim/{1}/spring_{{MTsim}}{1}_{2}", EngineDownloadPath, Name, archiveName));
                    paths.Add(string.Format("{0}buildbot/default/master/{1}/{3}/spring_{1}_{2}", EngineDownloadPath, Name, archiveName, platform));
                    paths.Add(string.Format("{0}buildbot/default/develop/{1}/{3}/spring_{{develop}}{1}_{2}",
                                            EngineDownloadPath,
                                            Name,
                                            archiveName,
                                            platform));
                    paths.Add(string.Format("{0}buildbot/default/release/{1}/{3}/spring_{{release}}{1}_{2}",
                                            EngineDownloadPath,
                                            Name,
                                            archiveName,
                                            platform));
                    paths.Add(string.Format("{0}buildbot/default/MTsim/{1}/{3}/spring_{{MTsim}}{1}_{2}",
                                            EngineDownloadPath,
                                            Name,
                                            archiveName,
                                            platform));

                    for (var i = 9; i >= -1; i--) {
                        var version = Name;
                        // if i==-1 we tested without version number
                        if (i >= 0) version = string.Format("{0}.{1}", Name, i);
                        paths.Add(string.Format("{0}spring_{1}.zip", EngineDownloadPath, version));
                    }

                    var source = paths.FirstOrDefault(VerifyFile);

                    if (source != null) {
                        var extension = source.Substring(source.LastIndexOf('.'));
                        var wc = new WebClient() { Proxy = null };
                        var target = Path.GetTempFileName() + extension;
                        wc.DownloadProgressChanged += (s, e) =>
                            {
                                Length = (int)(e.TotalBytesToReceive);
                                IndividualProgress = 10 + 0.8*e.ProgressPercentage;
                            };
                        wc.DownloadFileCompleted += (s, e) =>
                            {
                                if (e.Cancelled) {
                                    Trace.TraceInformation("Download {0} cancelled", Name);
                                    Finish(false);
                                }
                                else if (e.Error != null) {
                                    Trace.TraceWarning("Error downloading {0}: {1}", Name, e.Error);
                                    Finish(false);
                                }
                                else {
                                    Trace.TraceInformation("Installing {0}", source);
                                    var timer = new Timer((o) => { IndividualProgress += (100 - IndividualProgress)/10; }, null, 1000, 1000);

                                    if (extension == ".exe") {
                                        var p = new Process();
                                        p.StartInfo = new ProcessStartInfo(target,
                                                                           string.Format("/S /D={0}", springPaths.GetEngineFolderByVersion(Name)));
                                        p.Exited += (s2, e2) =>
                                            {
                                                timer.Dispose();
                                                if (p.ExitCode != 0) {
                                                    Trace.TraceWarning("Install of {0} failed: {1}", Name, p.ExitCode);
                                                    Finish(false);
                                                }
                                                else {
                                                    Trace.TraceInformation("Install of {0} complete", Name);
                                                    springPaths.SetEnginePath(springPaths.GetEngineFolderByVersion(Name));
                                                    Finish(true);
                                                }
                                            };

                                        p.EnableRaisingEvents = true;
                                        p.Start();
                                    }
                                    else {
                                        var targetDir = springPaths.GetEngineFolderByVersion(Name);
                                        if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                                        try {
                                            if (extension == ".7z") {
                                                var proc = Process.Start("7z", string.Format("x -r -y -o\"{1}\" \"{0}\"", target, targetDir));
                                                if (proc != null) proc.WaitForExit();
                                                if (proc == null || proc.ExitCode != 0) {
                                                    Trace.TraceWarning("7z extraction failed, fallback to SharpCompress");
                                                    ExtractArchive(target, targetDir);
                                                }
                                            }
                                            else {
                                                ExtractArchive(target, targetDir);
                                            }

                                            Trace.TraceInformation("Install of {0} complete", Name);
                                            springPaths.SetEnginePath(targetDir);
                                            Finish(true);
                                        } catch
                                        (Exception ex) {
                                            try {
                                                Directory.Delete(targetDir, true);
                                            } catch {}
                                            Trace.TraceWarning("Install of {0} failed: {1}", Name, ex);
                                            Finish(false);
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

        void ExtractArchive(string target, string targetDir) {
            using (var archive = ArchiveFactory.Open(target)) {
                long done = 0;
                var totalSize = archive.Entries.Count() + 1;
                archive.EntryExtractionEnd += (sender, args) =>
                    {
                        done++;
                        IndividualProgress = 90 + (10*done/totalSize);
                    };

                archive.WriteToDirectory(targetDir, ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);
            }
        }


        static bool VerifyFile(string url) {
            try {
                var request = WebRequest.Create(url);
                request.Method = "HEAD";
                request.Timeout = 4000;
                var res = request.GetResponse();
                var len = res.ContentLength;
                request.Abort();
                return len > 100000;
            } catch (Exception ex) {
                return false;
            }
        }
    }
}