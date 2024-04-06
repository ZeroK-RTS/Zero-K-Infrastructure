using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using PlasmaShared;


namespace ZkData
{
    public class SpringPaths
    {
        private List<string> dataDirectories = new List<string>();

        public enum PlatformType
        {
            win32,
            win64,
            linux32,
            linux64
        }

        public SpringPaths(string writableFolder, bool useMultipleDataFolders, bool allow64BitWindows, PlatformType? forcePlatform = null)
        {
            if (forcePlatform != null) Platform = forcePlatform.Value;
            else
            {
                
                Platform = PlatformType.win32;
                if (Environment.Is64BitOperatingSystem && allow64BitWindows) Platform = PlatformType.win64;
                if (Environment.OSVersion.Platform == PlatformID.Unix) Platform = Environment.Is64BitOperatingSystem ? PlatformType.linux64 : PlatformType.linux32;
            }

            WritableDirectory = writableFolder;

            dataDirectories = useMultipleDataFolders ? new List<string> { GetMySpringDocPath() } : new List<string>() { };
            if (!string.IsNullOrEmpty(writableFolder))
            {
                if (!Directory.Exists(writableFolder))
                {
                    try
                    {
                        Directory.CreateDirectory(writableFolder);
                    }
                    catch {}
                    ;
                }
                dataDirectories.Insert(0, writableFolder);
            }

            dataDirectories = dataDirectories.Where(Directory.Exists).Distinct().ToList();

            MakeFolders();
        }

        public void AddDataDirectories(IEnumerable<string> extraDataDirectories)
        {
            dataDirectories.AddRange(extraDataDirectories);
        }

        public string Cache { get; private set; }
        public ReadOnlyCollection<string> DataDirectories => dataDirectories.AsReadOnly();

        public string WritableDirectory { get; private set; }

        public bool UseSafeMode { get; set; }
        public PlatformType Platform { get; private set; }

        public string GetDedicatedServerPath(string engine)
        {
            if (!HasEngineVersion(engine))
            {
                Trace.TraceWarning("Requested invalid engine version: {0}", engine);
                return null;
            }

            return Path.Combine(GetEngineFolderByVersion(engine),
                Environment.OSVersion.Platform == PlatformID.Unix ? "spring-dedicated" : "spring-dedicated.exe");
        }

        public string GetSpringExecutablePath(string engine)
        {
            if (!HasEngineVersion(engine))
            {
                Trace.TraceWarning("Requested invalid engine version: {0}", engine);
                return null;
            }
            return Path.Combine(GetEngineFolderByVersion(engine), Environment.OSVersion.Platform == PlatformID.Unix ? "spring" : "spring.exe");
        }

        public event EventHandler<string> SpringVersionChanged;

        public void NotifyNewEngine(string engine)
        {
            SpringVersionChanged?.Invoke(this, engine);
        }

        private static string JoinDataDirectories(IEnumerable<string> input)
            => string.Join(Environment.OSVersion.Platform == PlatformID.Unix ? ":" : ";", input.Distinct());

        public string GetJoinedDataDirectoriesWithEngine(string engine)
        {
            return JoinDataDirectories(GetDataDirectoriesWithEngine(engine));
        }

        public List<string> GetDataDirectoriesWithEngine(string engine)
        {
            var list = DataDirectories.ToList();
            list.Add(GetEngineFolderByVersion(engine));
            return list;
        }

        public string GetEngineFolderByVersion(string version)
        {
            try
            {
                return Utils.MakePath(WritableDirectory, "engine", Platform.ToString(), version);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Cannot configure spring folder, WritableDirectory:{WritableDirectory}, EngineVersion:{version}, Exception:{ex}");
            }
        }

        public List<string> GetEngineList()
        {
            return
                new DirectoryInfo(Utils.MakePath(WritableDirectory, "engine", Platform.ToString())).GetDirectories().Select(x => x.Name).Where(HasEngineVersion).ToList();
        }


        public static string GetMySpringDocPath()
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                var path = Utils.MakePath(Environment.GetEnvironmentVariable("HOME"), ".spring");
                if (!IsDirectoryWritable(path))
                {
                    path = Utils.MakePath(Directory.GetCurrentDirectory(), ".spring");
                    IsDirectoryWritable(path);
                }
                return path;
            }
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
            return Utils.MakePath(WritableDirectory, "springsettings.cfg");
        }

        public bool HasEngineVersion(string version)
        {
            var path = GetEngineFolderByVersion(version);
            var exec = Path.Combine(path, Environment.OSVersion.Platform == PlatformID.Unix ? "spring" : "spring.exe");
            if (File.Exists(exec) && File.Exists(Path.Combine(path,"done.txt"))) return true;
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

        private void MakeFolders()
        {
            CreateFolder(Utils.MakePath(WritableDirectory, "games"));
            CreateFolder(Utils.MakePath(WritableDirectory, "maps"));
            CreateFolder(Utils.MakePath(WritableDirectory, "engine"));
            CreateFolder(Utils.MakePath(WritableDirectory, "packages"));
            CreateFolder(Utils.MakePath(WritableDirectory, "pool"));
            CreateFolder(Utils.MakePath(WritableDirectory, "demos"));
            CreateFolder(Utils.MakePath(WritableDirectory, "temp"));
            Cache = Utils.MakePath(WritableDirectory, "cache");
            CreateFolder(Cache);
        }


        private void CreateFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Unable to create folder {0} : {1}", path, ex.Message);
                }
            }
        }

        public static void SetEnvVar(ProcessStartInfo process, string key, string value)
        {
            Environment.SetEnvironmentVariable(key, value, EnvironmentVariableTarget.Process);
            if (process != null) process.EnvironmentVariables[key] = value;
        }

        public void SetDefaultEnvVars(ProcessStartInfo p, string engineVersion)
        {
            SetEnvVar(p, "SPRING_DATADIR", GetJoinedDataDirectoriesWithEngine(engineVersion));
            SetEnvVar(p, "SPRING_WRITEDIR", WritableDirectory);
            SetEnvVar(p, "SPRING_ISOLATED", WritableDirectory);
            SetEnvVar(p, "SPRING_NOCOLOR", "1");
        }

    }
}