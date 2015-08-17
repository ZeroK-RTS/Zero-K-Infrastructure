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
        public Config Config { get; set; }
        /// <summary>
        /// Engine to use
        /// </summary>
        public string Engine { get; set; }
        /// <summary>
        /// Mod to use as a base (override)
        /// </summary>
        public string Game { get; set; }
        /// <summary>
        /// Map to use (override)
        /// </summary>
        public string Map { get; set; }

        /// <summary>
        /// StartScript to use
        /// </summary>
        public StartScript StartScript { get; set; }


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
        public string Validate(PlasmaDownloader.PlasmaDownloader downloader, bool waitDownload = false) {
            if (string.IsNullOrEmpty(Engine)) return "Engine name not set";

            if (StartScript == null) return "Please select a start script - put start scripts in Benchmarks/Scripts folder";
            if (Config == null) return "Please select a config to use - create folders with configs in Benchmarks/Configs folder";

            var de = downloader.GetAndSwitchEngine(Engine);
            Download dm = null;

            if (!string.IsNullOrEmpty(Map)) {
                dm = downloader.GetResource(DownloadType.MAP, Map);
            }

            Download dg = null;
            if (!string.IsNullOrEmpty(Game)) {
                var ver = downloader.PackageDownloader.GetByTag(Game);
                if (ver != null) Game = ver.InternalName;
                dg = downloader.GetResource(DownloadType.MOD, Game);
                }

            if (waitDownload) {
                if (de != null) {
                    de.WaitHandle.WaitOne();
                    if (de.IsComplete == false) return "Failed download of engine " + de.Name;
                } 
                if (dm != null) {
                    dm.WaitHandle.WaitOne();
                    if (dm.IsComplete == false)return "Failed download of map " + dm.Name;
                }
                if (dg != null) {
                    dg.WaitHandle.WaitOne();
                    if (dg.IsComplete == false) return "Failed download of game " + dg.Name;
                } 
            }

            var sVal = StartScript.Validate(downloader, waitDownload);
            if (sVal != null) return sVal;

            return null;
        }

        public override string ToString() {
            return string.Format("{0} {1} {2} {3} {4}", Engine, StartScript, Game, Map, Config);
        }
    }
}