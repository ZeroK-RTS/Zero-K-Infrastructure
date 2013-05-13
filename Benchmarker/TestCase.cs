using System.Linq;
using PlasmaDownloader;

namespace Benchmarker
{
    /// <summary>
    /// Represents single test case (@case to be applied to benchmark)
    /// </summary>
    public class TestCase
    {
        /// <summary>
        /// Spring and mod configs
        /// </summary>
        public Config Config;
        /// <summary>
        /// Engine to use
        /// </summary>
        public string Engine;
        /// <summary>
        /// Mod to use as a base (override)
        /// </summary>
        public string Game;
        /// <summary>
        /// Map to use (override)
        /// </summary>
        public string Map;
        /// <summary>
        /// Whether to use multithreaded engine
        /// </summary>
        public bool UseMultithreaded;

        /// <summary>
        /// StartScript to use
        /// </summary>
        public StartScript StartScript;


        public TestCase(string engine, string game, string map, Config config, StartScript startScript) {
            Engine = engine;
            Game = game;
            Config = config;
            Map = map;
            StartScript = startScript;
        }

        
        /// <summary>
        /// Argument to use with --benchmark
        /// </summary>
        public int BenchmarkArg;


        /// <summary>
        /// Validates content - starts downloads, return null if all ok so far
        /// </summary>
        public string Validate(PlasmaDownloader.PlasmaDownloader downloader) {
            if (string.IsNullOrEmpty(Engine)) return "Engine name not set";

            if (StartScript == null) return "Please select a start script - put start scripts in Benchmarks/Scripts folder";
            if (Config == null) return "Please select a config to use - create folders with configs in Benchmarks/Configs folder";

            var de = downloader.GetAndSwitchEngine(Engine);
            if (de != null && de.IsComplete == false) return "Failed download of engine " + de.Name;

            if (!string.IsNullOrEmpty(Map)) {
                var dm = downloader.GetResource(DownloadType.MAP, Map);
                if (dm != null && dm.IsComplete == false) return "Failed download of map " + dm.Name;
            }

            if (!string.IsNullOrEmpty(Game)) {
                var dg = downloader.GetResource(DownloadType.MOD, Game);
                if (dg != null && dg.IsComplete == false) return "Failed download of game " + dg.Name;
            }

            var sVal = StartScript.Validate(downloader);
            if (sVal != null) return sVal;

            return null;
        }

        public override string ToString() {
            return string.Format("{0} {1} {2} {3} {4}", Engine, StartScript, Game, Map, Config);
        }
    }
}