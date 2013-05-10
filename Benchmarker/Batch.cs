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
        /// Cases to check
        /// </summary>
        public List<TestCase> TestCases = new List<TestCase>();
        public event Action<BatchRunResult> AllCompleted = (result) => { };
        public event Action<TestCase, Benchmark, string> RunCompleted = (run, benchmark, log) => { };

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
            var result = new BatchRunResult();
            foreach (var tr in TestCases) {
                foreach (var b in Benchmarks) {
                    if (isAborted) return;
                    b.ModifyModInfo(tr);
                    try {
                        run = new SpringRun();
                        var log = run.Start(new SpringPaths(null), tr, b);
                        result.AddRun(tr, b, log);
                        RunCompleted(tr, b, log);
                    } finally {
                        b.RestoreModInfo();
                    }
                }
            }
            if (isAborted) return;
            AllCompleted(result);
        }

        public void Save(string s) {
            File.WriteAllText(s, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        /// <summary>
        /// Validates content - downloads files
        /// </summary>
        public string Validate(PlasmaDownloader.PlasmaDownloader downloader) {
            if (!Benchmarks.Any()) return "No benchmarks selected";
            if (!TestCases.Any()) return "Please add testCase runs";

            foreach (var bench in Benchmarks) {
                var ret = bench.Validate(downloader);
                if (ret != null) return ret;
            }

            foreach (var run in TestCases) {
                var ret = run.Validate(downloader);
                if (ret != null) return ret;
            }
            return "OK";
        }

        void PostLoad() {
            Benchmarks = Benchmarks.Select(x => Benchmark.GetBenchmarks().First(y => y.Name == x.Name)).ToList();
            foreach (var tr in TestCases) tr.Config = Config.GetConfigs().Single(x => x.Name == tr.Config.Name);
        }
    }
}