using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using PlasmaDownloader;

namespace Benchmarker
{
    public class TestRun
    {
        public Config Config;
        public bool UseMultithreaded;
        public string Engine;
        public string Game;
        public string Map;


        public TestRun(string engine, string game, string map, Config config) {
            Engine = engine;
            Game = game;
            Config = config;
            Map = map;
        }

        public string Validate(PlasmaDownloader.PlasmaDownloader downloader) {
            if (string.IsNullOrEmpty(Engine)) return "Engine name not set";
            if (string.IsNullOrEmpty(Map)) return "Map name not set";
            if (string.IsNullOrEmpty(Game)) return "Game name not set";

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
            return string.Format("{0} {1} {2} {3}", Engine, Game, Map, Config);
        }

    }

    public class ScriptGenerator
    {
        public string Generate(Benchmark benchmark, TestRun test) {
            return File.ReadAllText(Path.Combine(benchmark.BenchmarkPath, "script.txt"));
        }
    }
}