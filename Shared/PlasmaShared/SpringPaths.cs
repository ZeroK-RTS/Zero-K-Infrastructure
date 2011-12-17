#region using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

#endregion

namespace PlasmaShared
{
    public class SpringPaths
    {
        string springVersion;
        readonly string writableFolderOverride;
        public string Cache { get; private set; }
        public List<string> DataDirectories { get; private set; }
        public string DedicatedServer { get; private set; }
        public string Executable { get; private set; }
        public string SpringVersion { get { return springVersion; } }
        public string UnitSyncDirectory { get; private set; }
        public string WritableDirectory { get; private set; }
        public event EventHandler SpringVersionChanged;

        public SpringPaths(string springPath, string version = null, string writableFolderOverride = null)
        {
            this.writableFolderOverride = writableFolderOverride;
            SetEnginePath(springPath);
            if (version != null) springVersion = version;
        }

        public void OverrideDedicatedServer(string path) {
            DedicatedServer = path;
        }

        public string GetEngineFolderByVersion(string version)
        {
            return Utils.MakePath(WritableDirectory, "engine", version);
        }


        public static string GetMySpringDocPath()
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix) return Utils.MakePath(Environment.GetEnvironmentVariable("HOME"), ".spring");
            else
            {
                var dir = Utils.MakePath(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Spring");
                if (!IsDirectoryWritable(dir))
                {
                    //if not writable - this should be writable
                    dir = Utils.MakePath(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Spring");
                }
                return dir;
            }
        }

        public string GetSpringConfigPath()
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix) return Utils.MakePath(Environment.GetEnvironmentVariable("HOME"), ".springrc");
            else return Utils.MakePath(WritableDirectory, "springsettings.cfg");
        }

        public bool HasEngineVersion(string version)
        {
            var path = GetEngineFolderByVersion(version);
            var exec = Path.Combine(path, "spring.exe");
            if (File.Exists(exec)) return GetSpringVersion(exec) == version;
            return false;
        }

        public static bool IsDirectoryWritable(string directory)
        {
            try
            {
                try
                {
                    if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                }
                catch
                {
                    return false;
                }

                var fullPath = Utils.GetAlternativeFileName(Path.Combine(directory, "test.dat"));
                File.WriteAllText(fullPath, "test");
                if (File.Exists(fullPath))
                {
                    File.ReadAllLines(fullPath);
                    File.Delete(fullPath);
                    return true;
                }
            }
            catch {}
            return false;
        }

        public void MakeFolders()
        {
            CreateFolder(Utils.MakePath(WritableDirectory, "mods"));
            CreateFolder(Utils.MakePath(WritableDirectory, "maps"));
            CreateFolder(Utils.MakePath(WritableDirectory, "packages"));
            CreateFolder(Utils.MakePath(WritableDirectory, "pool"));
            if (!string.IsNullOrEmpty(Cache)) CreateFolder(Cache);
        }


        public void SetEnginePath(string springPath)
        {
            DataDirectories = new List<string> { GetMySpringDocPath(), springPath };
            if (!string.IsNullOrEmpty(writableFolderOverride))
            {
                if (!Directory.Exists(writableFolderOverride))
                {
                    try
                    {
                        Directory.CreateDirectory(writableFolderOverride);
                    }
                    catch {}
                    ;
                }
                DataDirectories.Insert(0, writableFolderOverride);
            }

            DataDirectories = DataDirectories.Where(Directory.Exists).ToList();

            WritableDirectory = DataDirectories.First(IsDirectoryWritable);
            UnitSyncDirectory = springPath;
            if (!string.IsNullOrEmpty(springPath)) Executable = Utils.MakePath(springPath, "Spring.exe");
            else Executable = null;

            var ov = springVersion;
            if (string.IsNullOrEmpty(DedicatedServer)) springVersion = GetSpringVersion(Executable); // get spring verison does not work for dedicated
            if (ov != springVersion && SpringVersionChanged != null) SpringVersionChanged(this, EventArgs.Empty);

            Executable = Utils.MakePath(springPath, Environment.OSVersion.Platform == PlatformID.Unix ? "spring" : "spring.exe");
            DedicatedServer = Utils.MakePath(springPath, Environment.OSVersion.Platform == PlatformID.Unix ? "spring-dedicated" : "spring-dedicated.exe");
            Cache = Utils.MakePath(WritableDirectory, "cache","SD");
        }

        void CreateFolder(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }


        // can be null
        string DetectSpringConfigDataPath(string configPath)
        {
            string springDataPath = null;
            try
            {
                foreach (var line in File.ReadAllLines(configPath))
                {
                    var kvp = line.Split('=');
                    if (kvp.Length == 2 && kvp[0] == "SpringData" && kvp[1] != String.Empty) springDataPath = kvp[1];
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine("Unable to open springrc: " + e);
            }
            return springDataPath;
        }

        static string GetSpringVersion(string executablePath)
        {
            if (!File.Exists(executablePath)) return null;
            var LastPart =
                Path.GetDirectoryName(executablePath).Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            return LastPart;

            if (string.IsNullOrEmpty(executablePath)) throw new ApplicationException("Version can only be determined after executable path is known");
            try
            {
                var p = new Process();
                p.StartInfo = new ProcessStartInfo(executablePath, "--version")
                              { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
                p.Start();
                var data = p.StandardOutput.ReadToEnd();
                data = data.Trim();
                data = data.Replace("(MT-Sim)", "");
                data = data.Replace(" MT-Sim", "");
                var word = "";
                var match = Regex.Match(data, @"Spring [^ ]+ \(([^\)]+)\)");
                if (match.Success) word = match.Groups[1].Value;
                else
                {
                    match = Regex.Match(data, @"Spring ([^ ]+)");
                    if (match.Success) word = match.Groups[1].Value;
                }
                // parse word
                match = Regex.Match(word, "(\\d+\\.\\d+\\.\\d+)\\.\\d+$");
                if (match.Success) return match.Groups[1].Value;

                match = Regex.Match(word, "(\\d+)\\.\\d+$");
                if (match.Success) return match.Groups[1].Value;

                return word;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error determining spring version for {0}: {1}", executablePath, ex);
            }
            return null;
        }
    }
}