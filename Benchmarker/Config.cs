using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZkData;

namespace Benchmarker
{
    /// <summary>
    /// Represents spring and mod config (springsettings, and luaui/config etc)
    /// </summary>
    public class Config
    {
        static List<Config> allConfigs;
        public string ConfigPath;
        public string Name;

        public Config(string path) {
            ConfigPath = path;
            Name = Path.GetFileName(path);
        }


        public static IEnumerable<Config> GetConfigs(SpringPaths paths, bool refresh = false) {
            if (refresh) allConfigs = null;
            if (allConfigs != null) return allConfigs;
            allConfigs = new List<Config>();
            allConfigs = Batch.GetBenchmarkFolders(paths, "Configs").SelectMany(x => x.GetDirectories().Select(y => new Config(y.FullName))).ToList();
            return allConfigs;
        }


        public override string ToString() {
            return Name;
        }
    }
}