using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using PlasmaShared;
using ServiceStack.Text;

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
        public List<Benchmark> Benchmarks { get; set; }
        /// <summary>
        /// Cases to check
        /// </summary>
        public List<TestCase> TestCases { get; set; }
        public event Action<BatchRunResult> AllCompleted = (result) => { };
        public event Action<TestCase, Benchmark, string> RunCompleted = (run, benchmark, log) => { };

        public Batch() {
            Benchmarks = new List<Benchmark>();
            TestCases = new List<TestCase>();
        }

        public void Abort() {
            isAborted = true;
            if (run != null) run.Abort();
        }

        /// <summary>
        /// Returns folders of interest - for example if you set "games" it will look in all datadirs and current dir for "Games" and "Benchmarks/Games"
        /// </summary>
        public static List<DirectoryInfo> GetBenchmarkFolders(SpringPaths paths, string folderName) {
            var dirsToCheck = new List<string>(paths.DataDirectories);
            dirsToCheck.Add(Directory.GetCurrentDirectory());
            var ret = new List<DirectoryInfo>();
            foreach (var dir in dirsToCheck) {
                var sub = Path.Combine(dir, folderName);
                if (Directory.Exists(sub)) ret.Add(new DirectoryInfo(sub));

                var bsub = Path.Combine(dir, "Benchmarks", folderName);
                if (Directory.Exists(bsub)) ret.Add(new DirectoryInfo(bsub));
            }

            return ret;
        }

        public static Batch Load(string path, SpringPaths springPaths) {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            var batch = JsonSerializer.DeserializeFromString<Batch>(File.ReadAllText(path));
            batch.PostLoad(springPaths);
            return batch;
        }

        public void RunTests(SpringPaths paths) {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            isAborted = false;
            var result = new BatchRunResult();
            bool usingOptirun = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPTIRUN"));

            foreach (var tr in TestCases) {
                foreach (var b in Benchmarks) {
                    if (isAborted) return;
                    b.ModifyModInfo(tr);
                    string log = null;
                    try {
                        run = new SpringRun();
                        log = run.Start(paths, tr, b);
                    } catch (Exception ex) {
                        Trace.TraceError(ex.ToString());
                    } finally {
                        b.RestoreModInfo();
                    }
                    try {
                        if(usingOptirun) { // leave some time for optimus/primus to rest 
                           Thread.Sleep(5000);
                        }
                        result.AddRun(tr, b, log);
                        RunCompleted(tr, b, log);
                    } catch (Exception ex) {
                        Trace.TraceError(ex.ToString());
                    }
                }
            }
            if (isAborted) return;
            AllCompleted(result);
        }

        public void Save(string s) {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            File.WriteAllText(s, JsonSerializer.SerializeToString(this));
        }

        /// <summary>
        /// Validates content - downloads files
        /// </summary>
        public string Validate(PlasmaDownloader.PlasmaDownloader downloader, bool waitForDownload) {
            if (!Benchmarks.Any())
                return
                    "No benchmarks selected - please add benchmarks (mutators/games) into Gaqmes or Benchmarks/games folder - in the folder.sdd format";
            if (!TestCases.Any()) return "Please add test case runs using add button here";

            foreach (var bench in Benchmarks) {
                var ret = bench.Validate(downloader, waitForDownload);
                if (ret != null) return ret;
            }

            foreach (var run in TestCases) {
                var ret = run.Validate(downloader, waitForDownload);
                if (ret != null) return ret;
            }
            return "OK";
        }

        void PostLoad(SpringPaths paths) {
            Benchmarks =
                Benchmarks.Select(
                    x =>
                    Benchmark.GetBenchmarks(paths).SingleOrDefault(y => y.BenchmarkPath == x.BenchmarkPath) ??
                    Benchmark.GetBenchmarks(paths).First(y => y.Name == x.Name)).ToList();

            foreach (var tr in TestCases) {
                tr.Config = Config.GetConfigs(paths).SingleOrDefault(x => x.ConfigPath == tr.Config.ConfigPath) ??
                            Config.GetConfigs(paths).First(x => x.Name == tr.Config.Name);

                tr.StartScript = StartScript.GetStartScripts(paths).SingleOrDefault(x => x.ScriptPath == tr.StartScript.ScriptPath) ??
                                 StartScript.GetStartScripts(paths).First(x => x.Name == tr.StartScript.Name);
            }
        }
    }
}