using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Benchmarker
{
    public class Config
    {
        static List<Config> allConfigs;
        public string ConfigPath;
        public string Name;

        public Config(string path) {
            ConfigPath = path;
            Name = Path.GetFileName(path);
        }


        public static IEnumerable<Config> GetConfigs() {
            if (allConfigs != null) return allConfigs;
            var path = new DirectoryInfo(Directory.GetCurrentDirectory());
            do {
                var bd = path.GetDirectories().FirstOrDefault(x => string.Equals(x.Name, "Configs"));
                if (bd != null) {
                    allConfigs = bd.GetDirectories().Select(x => new Config(x.FullName)).ToList();
                    return allConfigs;
                }
                path = path.Parent;
            } while (path != null);
            return new List<Config>();
        }


        public override string ToString() {
            return Name;
        }
    }
}