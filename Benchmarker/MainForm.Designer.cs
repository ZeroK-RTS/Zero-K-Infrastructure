namespace Benchmarker
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.benchmarkList = new System.Windows.Forms.CheckedListBox();
            this.label2 = new System.Windows.Forms.Label();
            this.btnLoad = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tbBenchmarkArg = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.cmbScripts = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.cbConfigs = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.btnAddTest = new System.Windows.Forms.Button();
            this.tbMap = new System.Windows.Forms.TextBox();
            this.lbMap = new System.Windows.Forms.Label();
            this.tbEngine = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.tbGame = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.lbTestCases = new System.Windows.Forms.ListBox();
            this.label5 = new System.Windows.Forms.Label();
            this.btnRemoveRun = new System.Windows.Forms.Button();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.btnGraphs = new System.Windows.Forms.Button();
            this.btnLoadResults = new System.Windows.Forms.Button();
            this.tbResults = new System.Windows.Forms.TextBox();
            this.btnDataSheet = new System.Windows.Forms.Button();
            this.tbDownloads = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnBisect = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // benchmarkList
            // 
            this.benchmarkList.FormattingEnabled = true;
            this.benchmarkList.Location = new System.Drawing.Point(367, 59);
            this.benchmarkList.Name = "benchmarkList";
            this.benchmarkList.Size = new System.Drawing.Size(303, 154);
            this.benchmarkList.TabIndex = 0;
            this.benchmarkList.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.benchmarkList_ItemCheck);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(364, 43);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(117, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Tests (mutators) to use:";
            // 
            // btnLoad
            // 
            this.btnLoad.Location = new System.Drawing.Point(457, 10);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.Size = new System.Drawing.Size(75, 23);
            this.btnLoad.TabIndex = 5;
            this.btnLoad.Text = "Load";
            this.btnLoad.UseVisualStyleBackColor = true;
            this.btnLoad.Click += new System.EventHandler(this.btnLoad_Click);
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(367, 10);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 6;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.tbBenchmarkArg);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.cmbScripts);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.cbConfigs);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.btnAddTest);
            this.groupBox1.Controls.Add(this.tbMap);
            this.groupBox1.Controls.Add(this.lbMap);
            this.groupBox1.Controls.Add(this.tbEngine);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.tbGame);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Location = new System.Drawing.Point(20, 241);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(650, 180);
            this.groupBox1.TabIndex = 7;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Add a test case";
            // 
            // tbBenchmarkArg
            // 
            this.tbBenchmarkArg.Location = new System.Drawing.Point(465, 49);
            this.tbBenchmarkArg.Name = "tbBenchmarkArg";
            this.tbBenchmarkArg.Size = new System.Drawing.Size(100, 20);
            this.tbBenchmarkArg.TabIndex = 15;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(344, 52);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(115, 13);
            this.label6.TabIndex = 14;
            this.label6.Text = "--benchmark time (94+)";
            // 
            // cmbScripts
            // 
            this.cmbScripts.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbScripts.FormattingEnabled = true;
            this.cmbScripts.Location = new System.Drawing.Point(97, 22);
            this.cmbScripts.Name = "cmbScripts";
            this.cmbScripts.Size = new System.Drawing.Size(184, 21);
            this.cmbScripts.TabIndex = 12;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(31, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(60, 13);
            this.label1.TabIndex = 11;
            this.label1.Text = "Start script:";
            // 
            // cbConfigs
            // 
            this.cbConfigs.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbConfigs.FormattingEnabled = true;
            this.cbConfigs.Location = new System.Drawing.Point(97, 75);
            this.cbConfigs.Name = "cbConfigs";
            this.cbConfigs.Size = new System.Drawing.Size(184, 21);
            this.cbConfigs.TabIndex = 10;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(51, 78);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(40, 13);
            this.label7.TabIndex = 9;
            this.label7.Text = "Config:";
            // 
            // btnAddTest
            // 
            this.btnAddTest.Location = new System.Drawing.Point(196, 151);
            this.btnAddTest.Name = "btnAddTest";
            this.btnAddTest.Size = new System.Drawing.Size(85, 23);
            this.btnAddTest.TabIndex = 7;
            this.btnAddTest.Text = "Add test case";
            this.btnAddTest.UseVisualStyleBackColor = true;
            this.btnAddTest.Click += new System.EventHandler(this.btnAddTest_Click);
            // 
            // tbMap
            // 
            this.tbMap.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbMap.Location = new System.Drawing.Point(97, 128);
            this.tbMap.Name = "tbMap";
            this.tbMap.Size = new System.Drawing.Size(184, 20);
            this.tbMap.TabIndex = 5;
            // 
            // lbMap
            // 
            this.lbMap.AutoSize = true;
            this.lbMap.Location = new System.Drawing.Point(13, 131);
            this.lbMap.Name = "lbMap";
            this.lbMap.Size = new System.Drawing.Size(78, 13);
            this.lbMap.TabIndex = 4;
            this.lbMap.Text = "Map (override):";
            // 
            // tbEngine
            // 
            this.tbEngine.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbEngine.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.tbEngine.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.tbEngine.Location = new System.Drawing.Point(97, 49);
            this.tbEngine.Name = "tbEngine";
            this.tbEngine.Size = new System.Drawing.Size(184, 20);
            this.tbEngine.TabIndex = 1;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(48, 52);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(43, 13);
            this.label3.TabIndex = 0;
            this.label3.Text = "Engine:";
            // 
            // tbGame
            // 
            this.tbGame.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbGame.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.tbGame.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.tbGame.Location = new System.Drawing.Point(97, 102);
            this.tbGame.Name = "tbGame";
            this.tbGame.Size = new System.Drawing.Size(184, 20);
            this.tbGame.TabIndex = 3;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(13, 105);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(78, 13);
            this.label4.TabIndex = 2;
            this.label4.Text = "Mod (override):";
            // 
            // lbTestCases
            // 
            this.lbTestCases.FormattingEnabled = true;
            this.lbTestCases.Location = new System.Drawing.Point(21, 59);
            this.lbTestCases.Name = "lbTestCases";
            this.lbTestCases.Size = new System.Drawing.Size(303, 147);
            this.lbTestCases.TabIndex = 8;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(18, 43);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(118, 13);
            this.label5.TabIndex = 9;
            this.label5.Text = "Test cases to compare:";
            // 
            // btnRemoveRun
            // 
            this.btnRemoveRun.Location = new System.Drawing.Point(216, 212);
            this.btnRemoveRun.Name = "btnRemoveRun";
            this.btnRemoveRun.Size = new System.Drawing.Size(108, 23);
            this.btnRemoveRun.TabIndex = 10;
            this.btnRemoveRun.Text = "Remove test case";
            this.btnRemoveRun.UseVisualStyleBackColor = true;
            this.btnRemoveRun.Click += new System.EventHandler(this.btnRemoveRun_Click);
            // 
            // btnStart
            // 
            this.btnStart.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.btnStart.Location = new System.Drawing.Point(28, 10);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(75, 23);
            this.btnStart.TabIndex = 13;
            this.btnStart.Text = "START";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // btnStop
            // 
            this.btnStop.Enabled = false;
            this.btnStop.Location = new System.Drawing.Point(126, 10);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(75, 23);
            this.btnStop.TabIndex = 15;
            this.btnStop.Text = "STOP";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.btnGraphs);
            this.groupBox2.Controls.Add(this.btnLoadResults);
            this.groupBox2.Controls.Add(this.tbResults);
            this.groupBox2.Controls.Add(this.btnDataSheet);
            this.groupBox2.Location = new System.Drawing.Point(28, 503);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(642, 132);
            this.groupBox2.TabIndex = 16;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Benchmark results";
            // 
            // btnGraphs
            // 
            this.btnGraphs.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnGraphs.Enabled = false;
            this.btnGraphs.Location = new System.Drawing.Point(109, 103);
            this.btnGraphs.Name = "btnGraphs";
            this.btnGraphs.Size = new System.Drawing.Size(96, 23);
            this.btnGraphs.TabIndex = 3;
            this.btnGraphs.Text = "Open graphs";
            this.btnGraphs.UseVisualStyleBackColor = true;
            this.btnGraphs.Click += new System.EventHandler(this.btnGraphs_Click);
            // 
            // btnLoadResults
            // 
            this.btnLoadResults.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnLoadResults.Location = new System.Drawing.Point(540, 103);
            this.btnLoadResults.Name = "btnLoadResults";
            this.btnLoadResults.Size = new System.Drawing.Size(96, 23);
            this.btnLoadResults.TabIndex = 2;
            this.btnLoadResults.Text = "Load results";
            this.btnLoadResults.UseVisualStyleBackColor = true;
            this.btnLoadResults.Click += new System.EventHandler(this.btnLoadResults_Click);
            // 
            // tbResults
            // 
            this.tbResults.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbResults.Location = new System.Drawing.Point(7, 19);
            this.tbResults.Multiline = true;
            this.tbResults.Name = "tbResults";
            this.tbResults.ReadOnly = true;
            this.tbResults.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbResults.Size = new System.Drawing.Size(629, 78);
            this.tbResults.TabIndex = 1;
            // 
            // btnDataSheet
            // 
            this.btnDataSheet.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnDataSheet.Enabled = false;
            this.btnDataSheet.Location = new System.Drawing.Point(7, 103);
            this.btnDataSheet.Name = "btnDataSheet";
            this.btnDataSheet.Size = new System.Drawing.Size(96, 23);
            this.btnDataSheet.TabIndex = 0;
            this.btnDataSheet.Text = "Open datasheet";
            this.btnDataSheet.UseVisualStyleBackColor = true;
            this.btnDataSheet.Click += new System.EventHandler(this.btnDataSheet_Click);
            // 
            // tbDownloads
            // 
            this.tbDownloads.Location = new System.Drawing.Point(36, 444);
            this.tbDownloads.Multiline = true;
            this.tbDownloads.Name = "tbDownloads";
            this.tbDownloads.ReadOnly = true;
            this.tbDownloads.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbDownloads.Size = new System.Drawing.Size(628, 53);
            this.tbDownloads.TabIndex = 18;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(33, 428);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(143, 13);
            this.label8.TabIndex = 19;
            this.label8.Text = "Running content downloads:";
            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new System.Drawing.Point(568, 10);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(91, 23);
            this.btnRefresh.TabIndex = 20;
            this.btnRefresh.Text = "Refresh lists";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // btnBisect
            // 
            this.btnBisect.Location = new System.Drawing.Point(249, 10);
            this.btnBisect.Name = "btnBisect";
            this.btnBisect.Size = new System.Drawing.Size(75, 23);
            this.btnBisect.TabIndex = 21;
            this.btnBisect.Text = "Bisect";
            this.btnBisect.UseVisualStyleBackColor = true;
            this.btnBisect.Click += new System.EventHandler(this.btnBisect_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(702, 649);
            this.Controls.Add(this.btnBisect);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.tbDownloads);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.btnRemoveRun);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.lbTestCases);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnLoad);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.benchmarkList);
            this.MinimumSize = new System.Drawing.Size(718, 625);
            this.Name = "MainForm";
            this.Text = "Benchmarker";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckedListBox benchmarkList;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnLoad;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnAddTest;
        private System.Windows.Forms.TextBox tbMap;
        private System.Windows.Forms.Label lbMap;
        private System.Windows.Forms.TextBox tbGame;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox tbEngine;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ListBox lbTestCases;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button btnRemoveRun;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TextBox tbResults;
        private System.Windows.Forms.Button btnDataSheet;
        private System.Windows.Forms.ComboBox cbConfigs;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox tbDownloads;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Button btnLoadResults;
        private System.Windows.Forms.ComboBox cmbScripts;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnGraphs;
        private System.Windows.Forms.TextBox tbBenchmarkArg;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button btnBisect;
    }
}