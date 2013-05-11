using System;
using System.Diagnostics;
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
        string csvPath;
        string lastUsedBatchFolder = null;
        readonly PlasmaDownloader.PlasmaDownloader springDownloader;
        readonly SpringPaths springPaths;
        readonly SpringScanner springScanner;
        Batch testedBatch;
        BatchRunResult batchResult;

        public MainForm(SpringPaths paths = null, SpringScanner scanner = null, PlasmaDownloader.PlasmaDownloader downloader = null) {
            InitializeComponent();
            springPaths = paths ?? new SpringPaths(null, null, null);
            springScanner = scanner ?? new SpringScanner(springPaths);
            springScanner.Start();
            springDownloader = downloader ?? new PlasmaDownloader.PlasmaDownloader(new PlasmaConfig(), springScanner, springPaths);
            var timer = new Timer();
            timer.Tick += (sender, args) =>
                {
                    tbDownloads.Clear();
                    foreach (var d in springDownloader.Downloads.Where(x => x.IsComplete == null))
                        tbDownloads.AppendText(string.Format("{1:F0}% {0}  ETA: {2}  {3}\n",
                                                             d.Name,
                                                             d.IndividualProgress,
                                                             d.TimeRemaining,
                                                             d.IsComplete));
                };
            timer.Interval = 1000;
            timer.Enabled = true;

            tbEngine.Text = springPaths.SpringVersion;
        }

        public Batch CreateBatchFromGui() {
            var batch = new Batch()
            {
                TestCases = lbTestCases.Items.Cast<TestCase>().ToList(),
                Benchmarks = benchmarkList.CheckedItems.OfType<Benchmark>().ToList()
            };
            return batch;
        }

        void RefreshBenchmarks() {
            benchmarkList.Items.Clear();
            benchmarkList.Items.AddRange(Benchmark.GetBenchmarks(springPaths, true).ToArray());

            cbConfigs.Items.Clear();
            cbConfigs.Items.AddRange(Config.GetConfigs(springPaths, true).ToArray());

            cmbScripts.Items.Clear();
            cmbScripts.Items.AddRange(StartScript.GetStartScripts(springPaths, true).ToArray());
            if (cbConfigs.Items.Count > 0) cbConfigs.SelectedIndex = 0;
            if (cmbScripts.Items.Count > 0) cmbScripts.SelectedIndex = 0;
        }

        void MainForm_FormClosing(object sender, FormClosingEventArgs e) {
            springScanner.Dispose();
            springDownloader.Dispose();
        }

        void MainForm_Load(object sender, EventArgs e) {
            RefreshBenchmarks();
        }

        void benchmarkList_ItemCheck(object sender, ItemCheckEventArgs e) {
            ((Benchmark)benchmarkList.Items[e.Index]).Validate(springDownloader);
        }

        void btnAddTest_Click(object sender, EventArgs e) {
            var testCase = new TestCase(tbEngine.Text,
                                        tbGame.Text,
                                        tbMap.Text,
                                        cbConfigs.SelectedItem as Config,
                                        cmbScripts.SelectedItem as StartScript);
            var ret = testCase.Validate(springDownloader);
            if (ret != null) MessageBox.Show(ret);
            else lbTestCases.Items.Add(testCase);
        }

        void btnDataSheet_Click(object sender, EventArgs e) {
            if (!string.IsNullOrEmpty(csvPath)) Process.Start(csvPath);
        }

        void btnLoad_Click(object sender, EventArgs e) {
            try {
                var sd = new OpenFileDialog();
                sd.DefaultExt = ".batch";
                sd.Filter = ".batch (Benchmark batch configuration)|*.batch";
                if (sd.ShowDialog() == DialogResult.OK) {
                    var batch = Batch.Load(sd.FileName, springPaths);
                    if (batch != null) {
                        lbTestCases.Items.Clear();
                        lbTestCases.Items.AddRange(batch.TestCases.ToArray());

                        // prefill gui from batch
                        for (var i = 0; i < benchmarkList.Items.Count; i++) benchmarkList.SetItemChecked(i, batch.Benchmarks.Contains(benchmarkList.Items[i]));
                        var firstRun = batch.TestCases.First();
                        tbEngine.Text = firstRun.Engine;
                        tbMap.Text = firstRun.Map;
                        tbGame.Text = firstRun.Game;
                        cbConfigs.SelectedValue = firstRun.Config;

                        batch.Validate(springDownloader);

                        lastUsedBatchFolder = Path.GetDirectoryName(sd.FileName);
                    }
                    else MessageBox.Show("Batch file invalid");
                }
            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }

        void btnLoadResults_Click(object sender, EventArgs e) {
            var form = new OpenFileDialog
            {
                DefaultExt = ".json",
                Filter = ".json (Benchmark batch results)|*.json",
                InitialDirectory = lastUsedBatchFolder ?? springPaths.WritableDirectory,
                Title = "Load results of benchmark run"
            };
            if (form.ShowDialog() == DialogResult.OK) {
                batchResult = BatchRunResult.Load(form.FileName, out csvPath);
                if (batchResult != null) {
                    btnGraphs.Enabled = true;
                    btnDataSheet.Enabled = true;
                    var graph = new GraphsForm(batchResult);
                    graph.Show();
                    Process.Start(csvPath);
                    tbResults.Clear();
                    foreach (var run in batchResult.RunEntries) {
                        tbResults.AppendText(string.Format("== RUN {0} {1} ==\n", run.TestCase, run.Benchmark));
                        tbResults.AppendText(run.RawLog);
                    }
                }
            }
        }

        void btnRefresh_Click(object sender, EventArgs e) {
            RefreshBenchmarks();
        }

        void btnRemoveRun_Click(object sender, EventArgs e) {
            if (lbTestCases.SelectedIndex >= 0) lbTestCases.Items.RemoveAt(lbTestCases.SelectedIndex);
        }

        void btnSave_Click(object sender, EventArgs e) {
            var batch = CreateBatchFromGui();
            var sd = new SaveFileDialog();
            sd.FileName = "myTest.batch";
            sd.Filter = ".batch (Benchmark batch configuration)|*.batch";
            sd.OverwritePrompt = true;
            if (sd.ShowDialog() == DialogResult.OK) {
                batch.Save(sd.FileName);
                lastUsedBatchFolder = Path.GetDirectoryName(sd.FileName);
            }
        }

        void btnStart_Click(object sender, EventArgs e) {
            tbResults.Clear();

            testedBatch = CreateBatchFromGui();
            var validity = testedBatch.Validate(springDownloader);
            if (validity != "OK") {
                MessageBox.Show(validity);
                return;
            }

            testedBatch.RunCompleted += (run, benchmark, arg3) =>
                {
                    Invoke(new Action(() =>
                        {
                            tbResults.AppendText(string.Format("== RUN {0} {1} ==\n", run, benchmark));
                            tbResults.AppendText(arg3);
                        }));
                };

            testedBatch.AllCompleted += (result) =>
                {
                    batchResult = result;
                    string jsonPath;
                    result.SaveFiles(lastUsedBatchFolder ?? springPaths.WritableDirectory, out csvPath, out jsonPath);

                    //Process.Start(jsonPath);
                    Process.Start(csvPath);

                    Invoke(new Action(() =>
                        {
                            btnStart.Enabled = true;
                            btnStop.Enabled = false;
                            btnDataSheet.Enabled = true;
                            btnGraphs.Enabled = true;
                            var form = new GraphsForm(result);
                            form.Show();
                        }));
                };

            new Thread(() => { testedBatch.RunTests(springPaths); }).Start();

            btnStart.Enabled = false;
            btnStop.Enabled = true;
        }

        void btnStop_Click(object sender, EventArgs e) {
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            if (testedBatch != null) testedBatch.Abort();
        }

        private void btnGraphs_Click(object sender, EventArgs e)
        {
            if (batchResult != null) {
                var form = new GraphsForm(batchResult);
                form.Show();
            }

        }
    }

    public class PlasmaConfig: IPlasmaDownloaderConfig
    {
        public int RepoMasterRefresh { get { return 60; } }
        public string PackageMasterUrl { get { return "http://repos.springrts.com/"; } }
    }
}