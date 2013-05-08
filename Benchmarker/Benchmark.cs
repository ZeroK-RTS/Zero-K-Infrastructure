using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Benchmarker
{
    public class Benchmark
    {
        string benchmarkPath;
        readonly string name;

        public Benchmark(string path) {
            benchmarkPath = path;
            name = Path.GetFileName(path);
        }


        public static IEnumerable<Benchmark> GetBenchmarks() {
            var path = new DirectoryInfo(Directory.GetCurrentDirectory());
            do {
                var bd = path.GetDirectories().FirstOrDefault(x => string.Equals(x.Name, "Benchmarks"));
                if (bd != null) return bd.GetDirectories().Where(x => x.Name.EndsWith(".sdd")).Select(x => new Benchmark(x.FullName));
                path = path.Parent;
            } while (path != null);
            return new List<Benchmark>();
        }

        public override string ToString() {
            return name;
        }
    }
}