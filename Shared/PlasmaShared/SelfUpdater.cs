using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using ServiceStack.Text;

namespace ZkData
{
    public class SelfUpdater
    {
        Timer timer;
        int timerTick = 0;
        int timerTickInterval = 1;
        readonly string urlBase;
        readonly string urlUpdateName;
        public string CurrentVersion { get; private set; }
        public string LatestVersion { get; private set; }
        public int PeriodSeconds = 60 * 10;

        public Action<string> ProgramUpdated = s => { };
        public string TargetExecutablePath { get; set; }

        public class ManifestEntry
        {
            public string FileName { get; set; }
            public long Length { get; set; }
            public string Md5 { get; set; }
        }

        public class Manifest
        {
            public List<ManifestEntry> Entries { get; set; }
            public Manifest()
            {
                Entries = new List<ManifestEntry>();
            }
        }

        /// <summary>
        /// Creates self update checker
        /// </summary>
        /// <param name="updateName">Name of the program "Zero-K" or "Springie"</param>
        /// <param name="targetPath">Override target executable path</param>
        /// <param name="urlBase">Url location</param>
        public SelfUpdater(string updateName = null, string targetPath = null, string urlBase = null)
        {
            this.urlBase = urlBase;
            if (string.IsNullOrEmpty(this.urlBase)) this.urlBase = GlobalConst.SelfUpdaterBaseUrl;
            var entry = Assembly.GetEntryAssembly();
            TargetExecutablePath = targetPath ?? entry.Location;
            CurrentVersion = entry.GetName().Version.ToString();
            urlUpdateName = updateName ?? Path.GetFileNameWithoutExtension(TargetExecutablePath);
        }


        public bool CheckForUpdate(string targetFile = null, bool forced = false)
        {
            if (targetFile == null) targetFile = TargetExecutablePath;
            LatestVersion = GetLatestVersion();
            if (forced || (!string.IsNullOrEmpty(LatestVersion) && LatestVersion != CurrentVersion))
            {



                if (UpgradeFile(string.Format("{0}/{1}.exe", urlBase, urlUpdateName), targetFile))
                {
                    CurrentVersion = LatestVersion;
                    Trace.TraceInformation("{0} updated to {1}", targetFile, LatestVersion);
                    ProgramUpdated(targetFile);
                    return true;
                }
            }
            return false;
        }

        public static bool ReplaceFile(string filepath, byte[] data)
        {
            string newname = null;
            string bakName = null;
            try
            {
                newname = Utils.GetAlternativeFileName(filepath + ".new");
                File.WriteAllBytes(newname, data); // write new data
                bakName = Utils.GetAlternativeFileName(filepath + ".bak");
                File.Move(filepath, bakName); // copy current to bak
                File.Move(newname, filepath); // rename new
                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("File update failed {0} : {1}", filepath, ex.Message);
            }
            finally
            {
                try
                {
                    if (newname != null) File.Delete(newname);
                }
                catch { }
                foreach (var fb in Directory.GetFiles(Path.GetDirectoryName(bakName), Path.GetFileNameWithoutExtension(filepath) + "*.bak"))
                {
                    try
                    {
                        File.Delete(fb);
                    }
                    catch { }
                }
            }

            return false;
        }

        public void StartChecking()
        {
            StopChecking();
            timer = new Timer((o) =>
                {
                    timerTick++; // graduyally increase update check interval if update fails
                    if (timerTick >= timerTickInterval)
                    {
                        timerTick = 0;
                        if (!CheckForUpdate()) timerTickInterval *= 2;
                    }
                },
                              null,
                              100,
                              PeriodSeconds * 1000);
        }

        public void StopChecking()
        {
            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }
        }


        public static bool UpgradeFile(String url, String filepath)
        {
            var wg = new WebClient() { Proxy = null };
            try
            {
                var result = wg.DownloadData(new Uri(url));
                if (result != null && result.Length > 0) return ReplaceFile(filepath, result);
            }
            catch
            {
                Trace.TraceWarning("Download of {0} failed", url);
            }
            return false;
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

        public void GenerateManifest(params string[] extraFiles)
        {
            var files =
    AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic && Path.GetDirectoryName(x.Location) == Directory.GetCurrentDirectory()).Select(x => Path.GetFileName(x.Location)).ToList();
            foreach (var f in extraFiles) files.Add(f);


            var man = new Manifest();
            foreach (var fileName in files)
            {
                using (var file = File.OpenRead(fileName))
                {
                    var md5 = Hash.HashStream(file);
                    man.Entries.Add(new ManifestEntry() { Md5 = md5.ToString(), FileName = fileName, Length = new FileInfo(fileName).Length });
                }
            }
            var text = JsonSerializer.SerializeToString(man);

            File.WriteAllText(string.Format("{0}.manifest.txt", urlUpdateName), text);
        }
    }
}