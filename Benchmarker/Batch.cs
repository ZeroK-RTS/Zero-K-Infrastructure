using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using PlasmaShared;

namespace Benchmarker
{
    public class Batch
    {
        public List<Benchmark> Benchmarks = new List<Benchmark>();
        public List<TestRun> TestRuns = new List<TestRun>();

        public static Batch Load(string path) {
            var batch = JsonConvert.DeserializeObject<Batch>(File.ReadAllText(path));
            batch.PostLoad();
            return batch;
        }

        void PostLoad() {
            Benchmarks = Benchmarks.Select(x => Benchmark.GetBenchmarks().First(y => y.Name == x.Name)).ToList();
            foreach (var tr in TestRuns) tr.Config = Config.GetConfigs().Single(x => x.Name == tr.Config.Name);
        }

        public void Save(string s) {
            File.WriteAllText(s, JsonConvert.SerializeObject(this));
        }


        public void Start() {
            foreach (var tr in TestRuns) {
                foreach (var b in Benchmarks) {
                    var run = new SpringRun();
                    run.Start(new SpringPaths(null), tr, b);
                }

            }

        }

        public string Verify(PlasmaDownloader.PlasmaDownloader downloader) {
            if (!Benchmarks.Any()) return "No benchmarks selected";
            if (!TestRuns.Any()) return "Please add test runs";

            foreach (var run in TestRuns) {
                var ret = run.Validate(downloader);
                if (ret != null) return ret;
            }
            return "ALL OK, you can start batch";
        }
    }
}