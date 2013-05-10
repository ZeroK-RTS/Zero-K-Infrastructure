using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using PlasmaDownloader;
using PlasmaShared;
using Timer = System.Windows.Forms.Timer;

namespace Benchmarker
{
    public partial class MainForm: Form
    {
        readonly PlasmaDownloader.PlasmaDownloader downloader;
        readonly SpringScanner scanner;
        Batch testedBatch;

        public MainForm() {
            InitializeComponent();
            var paths = new SpringPaths(null, null, null);
            scanner = new SpringScanner(paths);
            scanner.Start();
            downloader = new PlasmaDownloader.PlasmaDownloader(new PlasmaConfig(), scanner, paths);
            var timer = new Timer();
            timer.Tick += (sender, args) =>
                {
                    tbDownloads.Clear();
                    foreach (var d in downloader.Downloads.Where(x => x.IsComplete == null))
                        tbDownloads.AppendText(string.Format("{1:F0}% {0}  ETA: {2}  {3}\n",
                                                             d.Name,
                                                             d.IndividualProgress,
                                                             d.TimeRemaining,
                                                             d.IsComplete));
                };
            timer.Interval = 1000;
            timer.Enabled = true;
        }

        public Batch CreateBatchFromGui() {
            var batch = new Batch()
            {
                TestRuns = lbTestRuns.Items.Cast<TestRun>().ToList(),
                Benchmarks = benchmarkList.CheckedItems.OfType<Benchmark>().ToList()
            };
            return batch;
        }

        void MainForm_FormClosing(object sender, FormClosingEventArgs e) {
            scanner.Dispose();
            downloader.Dispose();
        }

        void MainForm_Load(object sender, EventArgs e) {
            benchmarkList.Items.AddRange(Benchmark.GetBenchmarks().ToArray());
            cbConfigs.Items.AddRange(Config.GetConfigs().ToArray());
            if (cbConfigs.Items.Count > 0) cbConfigs.SelectedIndex = 0;
        }

        void benchmarkList_ItemCheck(object sender, ItemCheckEventArgs e) {
            ((Benchmark)benchmarkList.Items[e.Index]).Validate(downloader);
        }

        void btnAddTest_Click(object sender, EventArgs e) {
            var testRun = new TestRun(tbEngine.Text, tbGame.Text, tbMap.Text, (Config)cbConfigs.SelectedItem);
            var ret = testRun.Validate(downloader);
            if (ret != null) MessageBox.Show(ret);
            else lbTestRuns.Items.Add(testRun);
        }

        void btnLoad_Click(object sender, EventArgs e) {
            try {
                var sd = new OpenFileDialog();
                sd.DefaultExt = ".batch";
                if (sd.ShowDialog() == DialogResult.OK) {
                    var batch = Batch.Load(sd.FileName);
                    if (batch != null) {
                        lbBatchName.Text = Path.GetFileName(sd.FileName);
                        lbTestRuns.Items.Clear();
                        lbTestRuns.Items.AddRange(batch.TestRuns.ToArray());

                        // prefill gui from batch
                        for (var i = 0; i < benchmarkList.Items.Count; i++) benchmarkList.SetItemChecked(i, batch.Benchmarks.Contains(benchmarkList.Items[i]));
                        var firstRun = batch.TestRuns.First();
                        tbEngine.Text = firstRun.Engine;
                        tbMap.Text = firstRun.Map;
                        tbGame.Text = firstRun.Game;
                        cbConfigs.SelectedValue = firstRun.Config;

                        batch.Validate(downloader);
                    }
                    else MessageBox.Show("Batch file invalid");
                }
            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }

        void btnRemoveRun_Click(object sender, EventArgs e) {
            if (lbTestRuns.SelectedIndex >= 0) lbTestRuns.Items.RemoveAt(lbTestRuns.SelectedIndex);
        }

        void btnSave_Click(object sender, EventArgs e) {
            var batch = CreateBatchFromGui();
            var sd = new SaveFileDialog();
            sd.FileName = "myTest.batch";
            sd.OverwritePrompt = true;
            if (sd.ShowDialog() == DialogResult.OK) {
                batch.Save(sd.FileName);
                lbBatchName.Text = Path.GetFileName(sd.FileName);
            }
        }

        void btnStart_Click(object sender, EventArgs e) {
            testedBatch = CreateBatchFromGui();
            testedBatch.RunCompleted += (run, benchmark, arg3) =>
                {
                    Invoke(new Action(() =>
                        {
                            tbResults.AppendText(string.Format("== RUN {0} {1} ==\n", run, benchmark));
                            tbResults.AppendText(arg3);
                        }));
                };

            testedBatch.AllCompleted += () =>
                {
                    Invoke(new Action(() =>
                        {
                            btnStart.Enabled = true;
                            btnStop.Enabled = false;
                            }));
                };

            new Thread(() => { testedBatch.RunTests(); }).Start();

            btnStart.Enabled = false;
            btnStop.Enabled = true;
        }

        void btnStop_Click(object sender, EventArgs e) {
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            if (testedBatch !=null) testedBatch.Abort();
        }

        void btnVerify_Click(object sender, EventArgs e) {
            var batch = CreateBatchFromGui();
            MessageBox.Show(batch.Validate(downloader));
        }
    }

    public class PlasmaConfig: IPlasmaDownloaderConfig
    {
        public int RepoMasterRefresh { get { return 60; } }
        public string PackageMasterUrl { get { return "http://repos.springrts.com/"; } }
    }
}