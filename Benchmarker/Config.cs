using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Benchmarker
{
    public class Config
    {
        string benchmarkPath;
        readonly string name;

        public Config(string path) {
            benchmarkPath = path;
            name = Path.GetFileName(path);
        }

        public static IEnumerable<Config> GetConfigs() {
            var path = new DirectoryInfo(Directory.GetCurrentDirectory());
            do {
                var bd = path.GetDirectories().FirstOrDefault(x => string.Equals(x.Name, "Configs"));
                if (bd != null) return bd.GetDirectories().Where(x => x.Name.EndsWith(".cfg")).Select(x => new Config(x.FullName));
                path = path.Parent;
            } while (path != null);
            return new List<Config>();
        }

        public override string ToString() {
            return name;
        }
    }
}