using System.Linq;
using PlasmaDownloader;

namespace Benchmarker
{
    /// <summary>
    /// Represents single test case (variables to be applied to benchmark)
    /// </summary>
    public class TestRun
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


        public TestRun(string engine, string game, string map, Config config) {
            Engine = engine;
            Game = game;
            Config = config;
            Map = map;
        }


        /// <summary>
        /// Validates content - starts downloads, return null if all ok so far
        /// </summary>
        public string Validate(PlasmaDownloader.PlasmaDownloader downloader) {
            if (string.IsNullOrEmpty(Engine)) return "Engine name not set";

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

            return null;
        }

        public override string ToString() {
            return string.Format("{0} {1} {2} {3}", Engine, Game, Map, Config);
        }
    }
}