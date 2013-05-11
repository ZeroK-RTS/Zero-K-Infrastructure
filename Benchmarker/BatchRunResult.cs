using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Benchmarker
{
    public class BatchRunResult
    {
        static readonly string[] invalidKeys = new string[]
        {
            "Sent", "Received", "[CCollisionHandler] dis-/continuous tests", "Statistics for RectangleOptimizer", "AL lib: ALc.c:1808: alcCloseDevice()"
            , "[EPIC Menu] Error", "Game Over","Commanders Remaining"
        };
        public List<RunEntry> RunEntries = new List<RunEntry>();

        public void AddRun(TestCase testCase, Benchmark benchmark, string text) {
            if (string.IsNullOrEmpty(text)) return;
            var runEntry = new RunEntry() { TestCase = testCase, Benchmark = benchmark, RawLog = text };
            RunEntries.Add(runEntry);

            string gameId = null;

            foreach (var cycleline in text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)) {
                var line = cycleline;
                var gameframe = 0;

                if (line.StartsWith("[f=")) {
                    var idx = line.IndexOf("] ");
                    if (idx > 0) {
                        int.TryParse(line.Substring(3, idx - 3), out gameframe);
                        if (idx >= 0) line = line.Substring(idx + 2);
                    }
                }

                if (gameId != null) {
                    var match = Regex.Match(line, "!transmitlobby (.+):[ ]*([0-9.]+)");
                    if (match.Success) {
                        var key = match.Groups[1].Value.Trim();
                        var value = match.Groups[2].Value.Trim();
                        var valNum = 0.0;
                        if (!invalidKeys.Contains(key) && !key.StartsWith("Wind Range:") && !key.StartsWith("Skirmish AI") && !key.Contains(":")) if (double.TryParse(value, out valNum)) runEntry.RawValues.Add(new ValueEntry() { GameFrame = gameframe, Key = key, Value = valNum });
                    }
                }

                if (line.StartsWith("GameID: ") && gameId == null) gameId = line.Substring(8).Trim();
            }
        }


        public string GroupAndGenerateResultTable() {
            var sb = new StringBuilder();

            foreach (var entry in RunEntries) {
                foreach (var grp in entry.RawValues.GroupBy(x => x.Key)) {
                    var first = grp.First();
                    if (grp.Count() == 1) entry.GroupedValues.Add(first);
                    else {
                        entry.GroupedValues.Add(new ValueEntry() { Key = first.Key + "(avg)", Value = grp.Average(x => x.Value) });
                        entry.GroupedValues.Add(new ValueEntry() { Key = first.Key + "(min)", Value = grp.Min(x => x.Value) });
                        entry.GroupedValues.Add(new ValueEntry() { Key = first.Key + "(max)", Value = grp.Max(x => x.Value) });
                    }
                }
            }

            var cols = new List<ColEntry>();
            foreach (var run in RunEntries) {
                foreach (var val in run.GroupedValues) {
                    //var colName = string.Format("{0}_{1}", run.Benchmark.Name, val.Key);
                    var col = cols.FirstOrDefault(x => x.Benchmark == run.Benchmark && x.Key == val.Key);
                    if (col == null) {
                        col = new ColEntry() { Benchmark = run.Benchmark, Key = val.Key };
                        cols.Add(col);
                    }
                    col.Rows[run.TestCase] = val.Value;
                }
            }

            sb.Append("\"\";");
            foreach (var col in cols) sb.AppendFormat("\"{0}\";", col.Key);
            sb.Append("\n");

            sb.Append("\"\";");
            foreach (var col in cols) sb.AppendFormat("\"{0}\";", col.Benchmark.Name);
            sb.Append("\n");

            foreach (var tc in RunEntries.GroupBy(x => x.TestCase).Select(x => x.Key)) {
                sb.AppendFormat("\"{0}\";", tc);
                foreach (var col in cols) {
                    double val;
                    if (col.Rows.TryGetValue(tc, out val)) sb.AppendFormat("{0};", val);
                    else sb.AppendFormat(";");
                }
                sb.Append("\n");
            }

            return sb.ToString();
        }


        public void SaveFiles(string folder, out string csvPath, out string jsonPath) {
            var now = DateTime.Now;
            var csv = GroupAndGenerateResultTable();

            if (string.IsNullOrEmpty(folder)) folder = Directory.GetCurrentDirectory();

            var jsonFileName = Path.Combine(folder, string.Format("batchResult_{0:yyyy-MM-dd_HH-mm-ss}.json", now));
            var csvFileName = Path.Combine(folder, string.Format("batchResult_{0:yyyy-MM-dd_HH-mm-ss}.csv", now));

            File.WriteAllText(jsonFileName, JsonConvert.SerializeObject(this, Formatting.Indented));
            File.WriteAllText(csvFileName, csv);

            csvPath = csvFileName;
            jsonPath = jsonFileName;
        }

        public static BatchRunResult Load(string path, out string csvPath) {
            var ret = JsonConvert.DeserializeObject<BatchRunResult>(File.ReadAllText(path));
            if (ret != null) {
                csvPath = Path.ChangeExtension(path, ".csv");
            }
            else csvPath = null;
            return ret;
        }

        public class ColEntry
        {
            public Benchmark Benchmark;
            public string Key;
            public Dictionary<TestCase, double> Rows = new Dictionary<TestCase, double>();
        }

        public class RunEntry
        {
            public Benchmark Benchmark;
            public List<ValueEntry> GroupedValues = new List<ValueEntry>();
            public string RawLog;
            public List<ValueEntry> RawValues = new List<ValueEntry>();
            public TestCase TestCase;
        }

        public class ValueEntry
        {
            public int GameFrame;
            public string Key;
            public double Value;
        }
    }
}