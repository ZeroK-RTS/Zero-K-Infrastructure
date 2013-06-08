using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PlasmaDownloader;
using PlasmaShared;

namespace Benchmarker
{
    public partial class BisectForm: Form
    {
        Benchmark benchmark;
        readonly PlasmaDownloader.PlasmaDownloader downloader;
        string endVal;
        int engineEndIndex;
        List<string> engineList;
        int engineStartIndex;
        int modEndIndex;
        List<string> modList;
        int modStartIndex;
        readonly SpringPaths springPaths;

        string startVal;
        TestCase testCaseBase;
        string variableName;

        public BisectForm(SpringPaths springPaths, PlasmaDownloader.PlasmaDownloader downloader) {
            this.springPaths = springPaths;
            this.downloader = downloader;
            InitializeComponent();
        }

        public void InvokeIfNeeded(Action acc)
        {
            try
            {
                if (InvokeRequired) Invoke(acc);
                else acc();
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }


        public string RunBisect() {
            startVal = GetBisectValue(engineStartIndex, modStartIndex);
            endVal = GetBisectValue(engineEndIndex, modEndIndex);
            if (startVal == endVal) return string.Format("Bisect failed - both start and end return same value: {0}", startVal);

            if (startVal == null) return "Starting value is null";
            if (endVal == null) return "Ending value is null";

            var pivotEngine = (engineEndIndex + engineStartIndex)/2;
            var pivotMod = (modEndIndex + modStartIndex)/2;
            string val = null;
            while (pivotEngine != engineStartIndex || pivotEngine != engineEndIndex || pivotMod != modStartIndex || pivotMod != modEndIndex) {
                val = GetBisectValue(pivotEngine, pivotMod);
                if (val == startVal) {
                    engineStartIndex = pivotEngine;
                    modStartIndex = pivotMod;
                }
                else if (val == endVal) {
                    engineEndIndex = pivotEngine;
                    modEndIndex = pivotMod;
                }
                else if (val == null) {}
                else return string.Format("BISECT END unexpected value:{0} for engine:{1}, mod:{2}", val, engineList[pivotEngine], modList[pivotMod]);

                pivotEngine = (engineEndIndex + engineStartIndex)/2;
                pivotMod = (modEndIndex + modStartIndex)/2;
            }

            return string.Format("BISECT SUCCESS value:{0} for engine:{1}, mod:{2}", val, engineList[pivotEngine], modList[pivotMod]);
        }

        string GetBisectValue(int engineIndex, int modIndex) {
            var springRun = new SpringRun();

            testCaseBase.Engine = engineList[engineIndex];
            testCaseBase.Game = modList[modIndex];
            var ret = testCaseBase.Validate(downloader, true);
            if (ret != null) {
                InvokeIfNeeded(() => { tbBisectLog.AppendText(string.Format("Skipping test {0} - {1}\n", testCaseBase, ret)); });
                Trace.TraceError("Skipping test {0} - {1}", testCaseBase, ret);
                return null;
            }

            Trace.TraceInformation("Testing: {0}", testCaseBase);

            string retVal = null;
            springRun.LineAdded += s =>
                {
                    if (s != null) {
                        var match = Regex.Match(s, string.Format("!transmitlobby {0}[ ]*:(.*)", variableName), RegexOptions.IgnoreCase);
                        if (match.Success) {
                            retVal = match.Groups[1].Value;
                            springRun.Abort();
                        }
                    }
                };
            springRun.Start(springPaths, testCaseBase, benchmark);

            InvokeIfNeeded(() => { tbBisectLog.AppendText(string.Format("Test:{0}   value:{1}\n", testCaseBase, retVal)); });
            return retVal;
        }

        void SetupAutoComplete() {
            try {
                try {
                    engineList = EngineDownload.GetEngineList();
                    modList = downloader.PackageDownloader.Repositories.SelectMany(x => x.VersionsByTag.Keys).ToList();
                } catch (Exception ex) {
                    Trace.TraceError(ex.ToString());
                    InvokeIfNeeded(() =>
                        {
                            MessageBox.Show("Failed to get list: {0}", ex.Message);
                            Close();
                       });
                    return;
                }

                InvokeIfNeeded(() =>
                    {
                        tbEngine.AutoCompleteCustomSource.AddRange(engineList.ToArray());
                        tbEngineBisectTo.AutoCompleteCustomSource.AddRange(engineList.ToArray());
                        tbGame.AutoCompleteCustomSource.AddRange(modList.ToArray());
                        tbGameBisectTo.AutoCompleteCustomSource.AddRange(modList.ToArray());
                    });
            } catch (Exception ex) {
                Trace.TraceError(ex.ToString());
            }
        }

        void BisectForm_Load(object sender, EventArgs e) {
            Task.Factory.StartNew(SetupAutoComplete);
            cbBenchmark.Items.AddRange(Benchmark.GetBenchmarks(springPaths).ToArray());
            cbConfigs.Items.AddRange(Config.GetConfigs(springPaths).ToArray());
            cmbScripts.Items.AddRange(StartScript.GetStartScripts(springPaths).ToArray());
        }

        void btnAddTest_Click(object sender, EventArgs e) {
            benchmark = cbBenchmark.SelectedItem as Benchmark;
            if (benchmark == null) {
                MessageBox.Show("Please select a valid test");
                return;
            }
            variableName = tbBisectVariable.Text;

            if (string.IsNullOrEmpty(variableName)) {
                MessageBox.Show("Please select variable to bisect - test should send it using !transmitlobby variable:value");
                return;
            }
            testCaseBase = new TestCase(tbEngine.Text,
                                        tbGame.Text,
                                        tbMap.Text,
                                        cbConfigs.SelectedItem as Config,
                                        cmbScripts.SelectedItem as StartScript);

            var ret = testCaseBase.Validate(downloader);
            if (ret != null) {
                MessageBox.Show(ret);
                return;
            }

            engineStartIndex = engineList.IndexOf(tbEngine.Text);
            engineEndIndex = engineList.IndexOf(tbEngineBisectTo.Text);

            modStartIndex = modList.IndexOf(tbGame.Text);
            modEndIndex = modList.IndexOf(tbGameBisectTo.Text);

            if (modStartIndex == -1) {
                MessageBox.Show("Mod start value is not valid");
                return;
            }

            if (engineStartIndex == -1) {
                MessageBox.Show("Engine start value is not valid");
                return;
            }

            if (engineEndIndex == -1 && modEndIndex == -1) {
                MessageBox.Show("Please select at least one valid bisect to value (engine or mod)");
                return;
            }

            if (engineEndIndex == -1) engineEndIndex = engineStartIndex;
            if (modEndIndex == -1) modEndIndex = modStartIndex;

            btnBisect.Enabled = false;
            tbBisectLog.Clear();
            new Thread(() =>
                {
                    var result = RunBisect();
                    InvokeIfNeeded(() =>
                        {
                            MessageBox.Show(result);
                            btnBisect.Enabled = true;
                        });
                }).Start();
        }
    }
}