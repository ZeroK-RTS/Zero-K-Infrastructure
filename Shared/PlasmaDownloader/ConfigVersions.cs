using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using PlasmaDownloader.Packages;
using ZkData;

namespace PlasmaDownloader
{
    /// <summary>
    /// System to deploy/update configs and other files from chobby rapid
    /// </summary>
    public class ConfigVersions
    {
        public class ConfigVersionEntry
        {
            /// <summary>
            /// deploy only if platform is null or matching
            /// </summary>
            public string Platform { get; set; }

            public int VersionNumber { get; set; }

            public string SourcePath { get; set; }

            public string TargetPath { get; set; }
        }

        public List<ConfigVersionEntry> Versions = new List<ConfigVersionEntry>();

        private const string FilePathInChobby = "LuaMenu/configs/gameConfig/zk/defaultSettings/configversions.json";
        private const string FilePathOnDisk = "configversions.json";

        private static ConfigVersions LoadFromChobby(PackageDownloader.Version ver, SpringPaths paths)
        {
            var ms = ver?.ReadFile(paths, FilePathInChobby);
            if (ms != null) return TryDeserialize(Encoding.UTF8.GetString(ms.ToArray()));
            return null;
        }

        private static ConfigVersions LoadFromDisk(SpringPaths paths)
        {
            var filePath = Path.Combine(paths.WritableDirectory, FilePathOnDisk);
            if (File.Exists(filePath))
            {
                return TryDeserialize(File.ReadAllText(filePath));
            }
            return null;
        }

        private void SaveToDisk(SpringPaths paths)
        {
            File.WriteAllText(Path.Combine(paths.WritableDirectory, FilePathOnDisk), JsonConvert.SerializeObject(this));
        }


        private static ConfigVersions TryDeserialize(string content)
        {
            try
            {
                return JsonConvert.DeserializeObject<ConfigVersions>(content);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error deserializing config versions : {0}", ex);
                return null;
            }
        }

        public static void DeployAndResetConfigs(SpringPaths paths, PackageDownloader.Version ver)
        {
            if (ver != null)
            {
                var oldVers = LoadFromDisk(paths) ?? new ConfigVersions();
                var newVers = LoadFromChobby(ver, paths);
                bool hasError = false;

                if (newVers != null)
                {
                    foreach (
                        var versionEntry in newVers.Versions.Where(x => string.IsNullOrEmpty(x.Platform) || x.Platform == paths.Platform.ToString()))
                    {
                        try
                        {
                            var target = Path.Combine(paths.WritableDirectory, versionEntry.TargetPath);

                            if (
                                !oldVers.Versions.Any(
                                    x =>
                                        x.TargetPath == versionEntry.TargetPath && x.VersionNumber >= versionEntry.VersionNumber &&
                                        x.Platform == versionEntry.Platform) || !File.Exists(target))
                            {
                                var dirName = Path.GetDirectoryName(target);
                                if (!Directory.Exists(dirName)) Directory.CreateDirectory(dirName);

                                var content = ver.ReadFile(paths, versionEntry.SourcePath);
                                if (content != null)
                                {
                                    var targetBytes = content.ToArray();

                                    if (!File.Exists(target) || !Hash.ByteArrayEquals(File.ReadAllBytes(target), targetBytes)) { 
                                        File.WriteAllBytes(target, targetBytes);
                                    }
                                }
                                else File.Delete(target);
                            }
                        }
                        catch (Exception ex)
                        {
                            hasError = true;
                            Trace.TraceError("Error processing file deployment {0} : {1}", versionEntry.SourcePath, ex);
                        }
                    }
                }
                if (!hasError) (newVers ?? oldVers).SaveToDisk(paths);
            }
        }

    }
}