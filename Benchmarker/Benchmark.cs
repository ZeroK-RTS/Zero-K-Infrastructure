using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Benchmarker
{
    public class Benchmark
    {
        static List<Benchmark> allBenchmarks;
        public string BenchmarkPath;
        public string Name;


        public Benchmark(string path) {
            BenchmarkPath = path;
            Name = Path.GetFileName(path);
        }


        public static List<Benchmark> GetBenchmarks() {
            if (allBenchmarks != null) return allBenchmarks;
            var path = new DirectoryInfo(Directory.GetCurrentDirectory());
            do {
                var bd = path.GetDirectories().FirstOrDefault(x => string.Equals(x.Name, "Benchmarks"));
                if (bd != null) {
                    allBenchmarks = bd.GetDirectories().Where(x => x.Name.EndsWith(".sdd")).Select(x => new Benchmark(x.FullName)).ToList();
                    return allBenchmarks;
                }
                path = path.Parent;
            } while (path != null);
            return new List<Benchmark>();
        }


        public override string ToString() {
            return Name;
        }
    }
}