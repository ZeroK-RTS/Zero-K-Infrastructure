using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PlasmaDownloader;
using PlasmaShared.UnitSyncLib;

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
        public string BenchmarkPath;
        /// <summary>
        /// Short name (folder name)
        /// </summary>
        public string Name;


        public Benchmark(string path) {
            BenchmarkPath = path;
            Name = Path.GetFileName(path);
        }

        /// <summary>
        /// Gets all benchamrks and caches them. Looks for folder Benchmarks and traverses paths up until found
        /// </summary>
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


        /// <summary>
        /// Gets dependencies from modinfo.lua 
        /// </summary>
        public List<string> GetDependencies() {
            var match = Regex.Match(GetOrgModInfo(), "depend[ ]*=[ ]*{([^}]*)}");
            return
                match.Groups[1].Value.Split(',').Select(x => x.Trim().Trim(' ', '\r', '\n', '\'', '"')).Where(x => !string.IsNullOrEmpty(x)).ToList();
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
        /// Gets original startscript
        /// </summary>
        public string GetScript() {
            var file = Path.Combine(BenchmarkPath, "script.txt");
            if (File.Exists(file)) return File.ReadAllText(file);
            else return null;
        }

        /// <summary>
        /// Gets modified startscript for starting the game
        /// </summary>
        public string GetScriptForTestCase(TestCase test) {
            var script = GetScript();
            script = Regex.Replace(script, "(gametype=)([^;]*)", m => m.Groups[1] + Name);
            if (!string.IsNullOrEmpty(test.Map)) script = Regex.Replace(script, "(mapname=)([^;]*)", m => m.Groups[1] + test.Map);
            return script;
        }

        /// <summary>
        /// Gets map selected for by default in script.txt
        /// </summary>
        public string GetScriptMap() {
            var script = GetScript();
            if (script != null) {
                var match = Regex.Match(script, "mapname=([^;]+);");
                return match.Groups[1].Value;
            }
            else return null;
        }

        /// <summary>
        /// Gets full benchmark name as a mod for spring
        /// </summary>
        public string GetSpringName() {
            var info = GetOrgModInfo();
            string name = null;

            var matchName = Regex.Match(info, "name[ ]*=[ ]*([^,]+)");
            if (matchName.Success) name = matchName.Groups[1].Value;

            string version = null;
            var matchVersion = Regex.Match(info, "version[ ]*=[ ]*([^,]+)");
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
                                            m => m.Groups[1].Value + string.Join(",", deps.Select(x => string.Format("'{0}'", x))) + m.Groups[3]));
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
        public string Validate(PlasmaDownloader.PlasmaDownloader downloader) {
            foreach (var dep in
                GetDependencies().Where(x => !UnitSync.DependencyExceptions.Contains(x) && !GetBenchmarks().Any(y => y.GetSpringName() == x))) {
                var dl = downloader.GetResource(DownloadType.MOD, dep);
                if (dl != null && dl.IsComplete == false) return "Failed to download dependency mod " + dep;
            }
            var map = GetScriptMap();
            if (map != null) {
                var dl = downloader.GetResource(DownloadType.MAP, map);
                if (dl != null && dl.IsComplete == false) return "Failed to download map " + map;
            }
            return null;
        }

        public override string ToString() {
            return Name;
        }
    }
}