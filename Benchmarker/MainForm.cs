using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using PlasmaDownloader;
using PlasmaShared;

namespace Benchmarker
{
    public partial class MainForm: Form
    {
        readonly PlasmaDownloader.PlasmaDownloader downloader;

        public MainForm() {
            InitializeComponent();
            var paths = new SpringPaths(null, null, null);
            downloader = new PlasmaDownloader.PlasmaDownloader(new PlasmaConfig(), new SpringScanner(paths), paths);
            var timer = new Timer();
            timer.Tick += (sender, args) =>
                {
                    tbDownloads.Clear();
                    foreach (var d in downloader.Downloads.Where(x => x.IsComplete == null)) tbDownloads.AppendText(string.Format("{1:F0}% {0}  ETA: {2}\n", d.Name, d.IndividualProgress, d.TimeRemaining));
                };
            timer.Interval = 1000;
            timer.Enabled = true;
        }

        public Batch CreateBatchFromGui() {
            var batch = new Batch() { Name = tbBatchName.Text, TestRuns = lbTestRuns.Items.Cast<TestRun>().ToList() };
            return batch;
        }

        void MainForm_Load(object sender, EventArgs e) {
            benchmarkList.Items.AddRange(Benchmark.GetBenchmarks().ToArray());
            cbConfigs.Items.AddRange(Config.GetConfigs().ToArray());
            if (cbConfigs.Items.Count > 0) cbConfigs.SelectedIndex = 0;
        }

        void btnAddTest_Click(object sender, EventArgs e) {
            var testRun = new TestRun(tbEngine.Text,
                                      tbGame.Text,
                                      tbMap.Text,
                                      (Config)cbConfigs.SelectedItem,
                                      benchmarkList.SelectedItems.OfType<Benchmark>().ToList());
            var ret = testRun.Validate(downloader);
            if (ret != null) MessageBox.Show(ret);
            else lbTestRuns.Items.Add(testRun);
        }

        void btnLoad_Click(object sender, EventArgs e) {
            var sd = new OpenFileDialog();
            sd.DefaultExt = ".batch";
            if (sd.ShowDialog() == DialogResult.OK) {
                var batch = Batch.Load(sd.FileName);
                if (batch != null) {
                    tbBatchName.Text = batch.Name;
                    lbTestRuns.Items.Clear();
                    lbTestRuns.Items.AddRange(batch.TestRuns.ToArray());
                }
                else MessageBox.Show("Batch file invalid");
            }
        }

        void btnRemoveRun_Click(object sender, EventArgs e) {
            if (lbTestRuns.SelectedIndex >= 0) lbTestRuns.Items.RemoveAt(lbTestRuns.SelectedIndex);
        }

        void btnSave_Click(object sender, EventArgs e) {
            var batch = CreateBatchFromGui();
            var sd = new SaveFileDialog();
            sd.FileName = batch.Name + ".batch";
            sd.OverwritePrompt = true;
            if (sd.ShowDialog() == DialogResult.OK) batch.Save(sd.FileName);
        }

        void btnVerify_Click(object sender, EventArgs e) {
            var batch = CreateBatchFromGui();
            MessageBox.Show(batch.Verify(downloader));
        }
    }

    public class Batch
    {
        public string Name;
        public List<TestRun> TestRuns = new List<TestRun>();

        public static Batch Load(string path) {
            return JsonConvert.DeserializeObject<Batch>(path);
        }

        public void Save(string s) {
            File.WriteAllText(s, JsonConvert.SerializeObject(this));
        }

        public string Verify(PlasmaDownloader.PlasmaDownloader downloader) {
            if (string.IsNullOrEmpty(Name)) return "Please enter batch name";
            if (!TestRuns.Any()) return "Please add test runs";

            foreach (var run in TestRuns) {
                var ret = run.Validate(downloader);
                if (ret != null) return ret;
            }
            return "ALL OK, you can start batch";
        }
    }

    public class PlasmaConfig: IPlasmaDownloaderConfig
    {
        public int RepoMasterRefresh { get { return 60; } }
        public string PackageMasterUrl { get { return "http://repos.springrts.com/"; } }
    }
}