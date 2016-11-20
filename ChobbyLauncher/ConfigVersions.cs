using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using PlasmaDownloader.Packages;
using ZkData;

namespace ChobbyLauncher
{
    public class ConfigVersions
    {
        public Dictionary<string, int> Versions = new  Dictionary<string, int>();

        private const string FilePathInChobby = "LuaMenu/configs/gameConfig/zk/defaultSettings/configversions.json";
        private const string FilePathOnDisk = "configversions.json";

        public static ConfigVersions LoadFromChobby(PackageDownloader.Version ver, SpringPaths paths)
        {
            var ms = ver?.ReadFile(paths, FilePathInChobby);
            if (ms != null) return TryDeserialize(Encoding.UTF8.GetString(ms.ToArray()));
            return null;
        }

        public static ConfigVersions LoadFromDisk(SpringPaths paths)
        {
            var filePath = Path.Combine(paths.WritableDirectory, FilePathOnDisk);
            if (File.Exists(filePath))
            {
                TryDeserialize(File.ReadAllText(filePath));
            }
            return null;
        }

        public void SaveToDisk(SpringPaths paths)
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
                Trace.TraceWarning("Error deserializing config versions : {0}",ex);
                return null;
            }
        }

        public static void ResetConfigs(SpringPaths paths, PackageDownloader.Version ver)
        {
            if (ver != null)
            {
                var oldVers = LoadFromDisk(paths);
                var newVers = LoadFromChobby(ver, paths);
                if (oldVers != null && newVers != null)
                {
                    foreach (var kvp in newVers.Versions)
                    {
                        int oldVerNumber;
                        if (oldVers.Versions.TryGetValue(kvp.Key, out oldVerNumber) && oldVerNumber < kvp.Value)
                        {
                            try
                            {
                                File.Delete(Path.Combine(paths.WritableDirectory, kvp.Key));
                            }
                            catch (Exception ex)
                            {
                                Trace.TraceWarning("Error deleting config file {0} : {1}", kvp.Key, ex);
                            }
                        }
                    }
                }
                (newVers ?? new ConfigVersions()).SaveToDisk(paths);
            }
        }

    }
}