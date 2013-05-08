using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PlasmaDownloader;

namespace Benchmarker
{
    public class TestRun
    {
        public List<Benchmark> Benchmarks;
        public Config Config;
        public string Engine;
        public string Game;
        public string Map;


        public TestRun(string engine, string game, string map, Config config, List<Benchmark> benchmarks) {
            Engine = engine;
            Game = game;
            Config = config;
            Benchmarks = benchmarks;
            Map = map;
        }

        public string Validate(PlasmaDownloader.PlasmaDownloader downloader) {
            if (string.IsNullOrEmpty(Engine)) return "Engine name not set";
            if (string.IsNullOrEmpty(Map)) return "Map name not set";
            if (string.IsNullOrEmpty(Game)) return "Game name not set";
            if (!Benchmarks.Any()) return "No benchamrks selected";

            var waitList = new List<WaitHandle>();

            var de = downloader.GetAndSwitchEngine(Engine);
            var dm = downloader.GetResource(DownloadType.MAP, Map);
            var dg = downloader.GetResource(DownloadType.MOD, Game);
            
            if (de != null && de.IsComplete == false) return "Failed download of engine " + de.Name;
            if (dm != null && dm.IsComplete == false) return "Failed download of map " + dm.Name;
            if (dg != null && dg.IsComplete == false) return "Failed download of game " + dg.Name;
            return null;
        }

        public override string ToString() {
            return string.Format("{0} {1} {2} {3} {4}", Engine, Game, Map, Config, string.Join(",", Benchmarks));
        }
    }
}