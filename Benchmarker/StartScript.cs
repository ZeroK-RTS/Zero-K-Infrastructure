using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PlasmaDownloader;
using ZkData;

namespace Benchmarker
{
    public class StartScript
    {
        static List<StartScript> allStartScripts;
        /// <summary>
        /// Short name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Full path to StartScript
        /// </summary>
        public string ScriptPath { get; set; }

        public StartScript(string path) {
            ScriptPath = path;
            Name = Path.GetFileNameWithoutExtension(path);
        }

        public static IEnumerable<StartScript> GetStartScripts(SpringPaths paths, bool refresh = false) {
            if (refresh) allStartScripts = null;
            if (allStartScripts != null) return allStartScripts;
            allStartScripts = new List<StartScript>();
            allStartScripts =
                Batch.GetBenchmarkFolders(paths, "Scripts").SelectMany(x => x.GetFiles("*.txt").Select(y => new StartScript(y.FullName))).ToList();
            return allStartScripts;
        }


        /// <summary>
        /// Gets original startscript
        /// </summary>
        public string GetScript() {
            return File.ReadAllText(ScriptPath);
        }

        /// <summary>
        /// Gets modified startscript for starting the game
        /// </summary>
        public string GetScriptForTestCase(TestCase test, Benchmark benchmark) {
            var script = GetScript();
            script = Regex.Replace(script, "(gametype=)([^;]*)", m => m.Groups[1] + benchmark.Name, RegexOptions.IgnoreCase);
            if (!string.IsNullOrEmpty(test.Map)) script = Regex.Replace(script, "(mapname=)([^;]*)", m => m.Groups[1] + test.Map, RegexOptions.IgnoreCase);
            return script;
        }

        /// <summary>
        /// Gets map selected for by default in StartScript.txt
        /// </summary>
        public string GetScriptMap() {
            var script = GetScript();
            if (script != null) {
                var match = Regex.Match(script, "mapname=([^;]+);", RegexOptions.IgnoreCase);
                return match.Groups[1].Value;
            }
            else return null;
        }


        public string Validate(PlasmaDownloader.PlasmaDownloader downloader, bool waitForDownload = false) {
            var map = GetScriptMap();
            if (map != null) {
                var dl = downloader.GetResource(DownloadType.MAP, map);
                if (waitForDownload) {
                    if (dl != null) {
                        dl.WaitHandle.WaitOne();
                        if (dl.IsComplete == false) return "Failed to download map " + map;
                    } 
                }
            }
            return null;
        }


        public override string ToString() {
            return Name;
        }
    }
}