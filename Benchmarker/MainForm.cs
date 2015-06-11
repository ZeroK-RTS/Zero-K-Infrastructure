using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PlasmaDownloader;
using ZkData;
using Timer = System.Windows.Forms.Timer;

namespace Benchmarker
{
    public partial class MainForm: Form
    {
        string lastUsedBatchFolder = null;
        readonly PlasmaDownloader.PlasmaDownloader springDownloader;
        readonly SpringPaths springPaths;
        readonly SpringScanner springScanner;
        Batch testedBatch;
        BatchRunResult batchResult;
        private Timer timer;

        public static void SafeStart(string path, string args = null)
        {
            try
            {
                var pi = new ProcessStartInfo(path, args);
                pi.WorkingDirectory = Path.GetDirectoryName(path);
                pi.UseShellExecute = true;
                Process.Start(pi);
            }
            catch (Exception ex)
            {
                MessageBox.Show(path + ": " + ex.Message, "Opening failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        public MainForm(SpringPaths paths = null, SpringScanner scanner = null, PlasmaDownloader.PlasmaDownloader downloader = null) {
            InitializeComponent();
            if (paths != null) springPaths = paths;
            else springPaths = new SpringPaths(null, writableFolderOverride: null);
            if (scanner != null) springScanner = scanner;
            else {
                springScanner = new SpringScanner(springPaths);
                springScanner.Start();
            }
            if (downloader != null)  springDownloader = downloader;
            else springDownloader = new PlasmaDownloader.PlasmaDownloader(new PlasmaConfig(), springScanner, springPaths);

            timer = new Timer();
            timer.Tick += (sender, args) =>
                {
                    tbDownloads.Clear();
                    foreach (var d in springDownloader.Downloads.Where(x => x.IsComplete == null))
                        tbDownloads.AppendText(string.Format("{1:F0}% {0}  ETA: {2}  {3}\n",
                                                             d.Name,
                                                             d.TotalProgress,
                                                             d.TimeRemaining,
                                                             d.IsComplete));
                };
            timer.Interval = 1000;
            timer.Enabled = true;

            

            tbEngine.Text = springPaths.SpringVersion;
        }


        public void InvokeIfNeeded(Action acc) {
            try {
                if (InvokeRequired) Invoke(acc);
                else acc();
            } catch (Exception ex) {
                Trace.TraceError(ex.ToString());
            }
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
        }

        void MainForm_FormClosing(object sender, FormClosingEventArgs e) {
            springScanner.Dispose();
            springDownloader.Dispose();
            timer.Dispose(); //Note! timer will run even when you close the Benchmarker window, so it need to Dispose()
        }

        void MainForm_Load(object sender, EventArgs e) {
            Task.Factory.StartNew(SetupAutoComplete);
            RefreshBenchmarks();
        }

        void benchmarkList_ItemCheck(object sender, ItemCheckEventArgs e) {
            ((Benchmark)benchmarkList.Items[e.Index]).Validate(springDownloader, false);
        }

        void btnAddTest_Click(object sender, EventArgs e) {
            var testCase = new TestCase(tbEngine.Text,
                                        tbGame.Text,
                                        tbMap.Text,
                                        cbConfigs.SelectedItem as Config,
                                        cmbScripts.SelectedItem as StartScript);
            int arg;
            int.TryParse(tbBenchmarkArg.Text, out arg);
            testCase.BenchmarkArg = arg;
            var ret = testCase.Validate(springDownloader);
            if (ret != null) MessageBox.Show(ret);
            else lbTestCases.Items.Add(testCase);
        }

        void btnDataSheet_Click(object sender, EventArgs e) {
            try {
                SafeStart(batchResult.SaveAndGetCsvFileName());
            } catch (Exception ex) {
                Trace.TraceError(ex.ToString());
            }
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

                        batch.Validate(springDownloader, false);

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
                batchResult = BatchRunResult.Load(form.FileName);
                if (batchResult != null) {
                    btnGraphs.Enabled = true;
                    btnDataSheet.Enabled = true;

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


        void SetupAutoComplete()
        {
            try {
                List<string> engineList = new List<string>();
                List<string> modList = new List<string>();
                try
                {
                    engineList = EngineDownload.GetEngineList();
                    modList = springDownloader.PackageDownloader.Repositories.SelectMany(x => x.VersionsByTag.Keys).ToList();
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }

                InvokeIfNeeded(() =>
                {
                    tbEngine.AutoCompleteCustomSource.AddRange(engineList.ToArray());
                    tbGame.AutoCompleteCustomSource.AddRange(modList.ToArray());
                });
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
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
            var validity = testedBatch.Validate(springDownloader, false);
            if (validity != "OK") {
                MessageBox.Show(validity);
                return;
            }

            testedBatch.RunCompleted += TestedBatchOnRunCompleted;
            testedBatch.AllCompleted += TestedBatchOnAllCompleted;

            new Thread(() =>
                {
                    testedBatch.Validate(springDownloader, true);
                    testedBatch.RunTests(springPaths);
                }).Start();

            btnStart.Enabled = false;
            btnStop.Enabled = true;
        }

        void TestedBatchOnAllCompleted(BatchRunResult result) {
            result.SaveFiles(lastUsedBatchFolder ?? springPaths.WritableDirectory);
            batchResult = result;
            InvokeIfNeeded(() =>
            {
                btnStart.Enabled = true;
                btnStop.Enabled = false;
                btnDataSheet.Enabled = true;
                btnGraphs.Enabled = true;
                MessageBox.Show("Test batch run complete, please open the graph and datasheet by pressing buttons on the left");
            });

        }

        void TestedBatchOnRunCompleted(TestCase run, Benchmark benchmark, string arg3) {
            var stringToAppend = string.Format("== RUN {0} {1} ==\n", run, benchmark) + arg3;
                        InvokeIfNeeded(() => tbResults.AppendText(stringToAppend));
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

        private void btnBisect_Click(object sender, EventArgs e) {
            var bisForm = new BisectForm(springPaths, springDownloader );
            bisForm.Show();
        }

    }

    public class PlasmaConfig: IPlasmaDownloaderConfig
    {
        public int RepoMasterRefresh { get { return 60; } }
        public string PackageMasterUrl { get { return "http://repos.springrts.com/"; } }
    }
}