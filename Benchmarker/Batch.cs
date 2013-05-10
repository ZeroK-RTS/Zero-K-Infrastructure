using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using PlasmaShared;

namespace Benchmarker
{
    /// <summary>
    /// Full batch of tests that can be saved,loaded, executed nad measured
    /// </summary>
    public class Batch
    {
        bool isAborted;
        SpringRun run;
        /// <summary>
        /// Benchmark mutators to use
        /// </summary>
        public List<Benchmark> Benchmarks = new List<Benchmark>();
        /// <summary>
        /// Runs to perform (variables to check)
        /// </summary>
        public List<TestRun> TestRuns = new List<TestRun>();
        public event Action AllCompleted = () => { };
        public event Action<TestRun, Benchmark, string> RunCompleted = (run, benchmark, log) => { };

        public void Abort() {
            isAborted = true;
            if (run != null) run.Abort();
        }

        public static Batch Load(string path) {
            var batch = JsonConvert.DeserializeObject<Batch>(File.ReadAllText(path));
            batch.PostLoad();
            return batch;
        }

        public void RunTests() {
            isAborted = false;
            foreach (var tr in TestRuns) {
                foreach (var b in Benchmarks) {
                    if (isAborted) return;
                    b.ModifyModInfo(tr);
                    run = new SpringRun();
                    var lines = run.Start(new SpringPaths(null), tr, b);
                    RunCompleted(tr, b, lines);
                }
            }
            if (isAborted) return;
            AllCompleted();
        }

        public void Save(string s) {
            File.WriteAllText(s, JsonConvert.SerializeObject(this));
        }

        /// <summary>
        /// Validates content - downloads files
        /// </summary>
        public string Validate(PlasmaDownloader.PlasmaDownloader downloader) {
            if (!Benchmarks.Any()) return "No benchmarks selected";
            if (!TestRuns.Any()) return "Please add test runs";

            foreach (var bench in Benchmarks) {
                var ret = bench.Validate(downloader);
                if (ret != null) return ret;
            }

            foreach (var run in TestRuns) {
                var ret = run.Validate(downloader);
                if (ret != null) return ret;
            }
            return "ALL OK, you can start batch";
        }

        void PostLoad() {
            Benchmarks = Benchmarks.Select(x => Benchmark.GetBenchmarks().First(y => y.Name == x.Name)).ToList();
            foreach (var tr in TestRuns) tr.Config = Config.GetConfigs().Single(x => x.Name == tr.Config.Name);
        }
    }
}