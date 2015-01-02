using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PlasmaDownloader;
using ZkData;
using ZkData.UnitSyncLib;

namespace Benchmarker
{
    /// <summary>
    /// Represents benchmarking mutator
    /// </summary>
    public class Benchmark
    {
        static List<Benchmark> allBenchmarks;
        /// <summary>
        /// Full path to mutator
        /// </summary>
        public string BenchmarkPath { get; set; }
        /// <summary>
        /// Short name (folder name)
        /// </summary>
        public string Name { get; set; }


        public Benchmark(string path) {
            BenchmarkPath = path;
            Name = Path.GetFileName(path);
        }

        /// <summary>
        /// Gets all benchamrks and caches them. Looks for folder Benchmarks and traverses paths up until found
        /// </summary>
        public static List<Benchmark> GetBenchmarks(SpringPaths paths, bool refresh = false) {
            if (refresh) allBenchmarks = null;
            if (allBenchmarks != null) return allBenchmarks;
            allBenchmarks = new List<Benchmark>();
            allBenchmarks = Batch.GetBenchmarkFolders(paths, "games").SelectMany(x => x.GetDirectories("*.sdd").Select(y=> new Benchmark(y.FullName))).ToList();
            return allBenchmarks;
        }




        /// <summary>
        /// Gets dependencies from modinfo.lua 
        /// </summary>
        public List<string> GetDependencies() {
            var match = Regex.Match(GetOrgModInfo(), "depend[ ]*=[ ]*{([^}]*)}", RegexOptions.IgnoreCase);
            return
                match.Groups[1].Value.Split(',').Select(x => x.Trim().Trim('[', ']',' ', '\r', '\n').Trim('\'', '"' )).Where(x => !string.IsNullOrEmpty(x)).ToList();
        }

        /// <summary>
        /// Save original modinfo to .org file - test runs need to modify modinfo
        /// </summary>
        public string GetOrgModInfo() {
            var orgFile = Path.Combine(BenchmarkPath, "modinfo.lua.org");
            var modFile = Path.Combine(BenchmarkPath, "modinfo.lua");
            if (File.Exists(orgFile)) return File.ReadAllText(orgFile);
            return File.ReadAllText(modFile);
        }


        /// <summary>
        /// Gets full benchmark name as a mod for spring
        /// </summary>
        public string GetSpringName() {
            var info = GetOrgModInfo();
            string name = null;

            var matchName = Regex.Match(info, "name[ ]*=[ ]*([^,]+)", RegexOptions.IgnoreCase);
            if (matchName.Success) name = matchName.Groups[1].Value;

            string version = null;
            var matchVersion = Regex.Match(info, "version[ ]*=[ ]*([^,]+)", RegexOptions.IgnoreCase);
            if (matchVersion.Success) version = matchVersion.Groups[1].Value;

            if (name != null) {
                if (version == null) return name;
                else return string.Format("{0} {1}", name, version);
            }
            else return null;
        }


        /// <summary>
        /// Modify modinfo to add or change dependencies. If mod already depends on game which starts with same word as testcase game, replace it, otherwise append it
        /// </summary>
        public void ModifyModInfo(TestCase testCase) {
            var orgFile = Path.Combine(BenchmarkPath, "modinfo.lua.org");
            var modFile = Path.Combine(BenchmarkPath, "modinfo.lua");
            File.Copy(modFile, orgFile, true);


            var deps = GetDependencies();

            if (!string.IsNullOrEmpty(testCase.Game)) {
                var firstWord = testCase.Game.Split(' ').First();
                var toReplace = deps.FirstOrDefault(x => x.StartsWith(firstWord + " "));
                if (toReplace != null) deps.Remove(toReplace);
                deps.Add(testCase.Game);
            }

            File.WriteAllText(Path.Combine(BenchmarkPath, "modinfo.lua"),
                              Regex.Replace(GetOrgModInfo(),
                                            "(depend[ ]*=[ ]*{)([^}]*)(})",
                                            m => m.Groups[1].Value + string.Join(",", deps.Select(x => string.Format("'{0}'", x))) + m.Groups[3],RegexOptions.IgnoreCase));
        }

        /// <summary>
        /// Restores modinfo to original state
        /// </summary>
        public void RestoreModInfo() {
            var orgFile = Path.Combine(BenchmarkPath, "modinfo.lua.org");
            var modFile = Path.Combine(BenchmarkPath, "modinfo.lua");
            File.Copy(orgFile, modFile, true);
            File.Delete(orgFile);
        }


        /// <summary>
        /// Validate content files - starts downloads
        /// </summary>
        public string Validate(PlasmaDownloader.PlasmaDownloader downloader, bool waitForDownload) {
            foreach (var dep in
                GetDependencies().Where(x => !UnitSync.DependencyExceptions.Contains(x))) {
                if (GetBenchmarks(downloader.SpringPaths).Any(y => y.GetSpringName() == dep || y.Name == dep)) continue;
                var dl = downloader.GetResource(DownloadType.MOD, dep);
                if (dl != null && waitForDownload) {
                    dl.WaitHandle.WaitOne();
                    if (dl.IsComplete == false) return "Failed to download dependency mod " + dep;
                }
            }
            return null;
        }

        public override string ToString() {
            return Name;
        }
    }
}