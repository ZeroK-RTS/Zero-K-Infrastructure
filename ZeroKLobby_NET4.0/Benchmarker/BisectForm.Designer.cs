namespace Benchmarker
{
    partial class BisectForm
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.cbBenchmark = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.tbGameBisectTo = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.tbEngineBisectTo = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.tbBisectVariable = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.cbMultiThread = new System.Windows.Forms.CheckBox();
            this.cmbScripts = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.cbConfigs = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.btnBisect = new System.Windows.Forms.Button();
            this.tbMap = new System.Windows.Forms.TextBox();
            this.lbMap = new System.Windows.Forms.Label();
            this.tbEngine = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.tbGame = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.tbBisectLog = new System.Windows.Forms.TextBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.tbBisectLog);
            this.groupBox1.Controls.Add(this.cbBenchmark);
            this.groupBox1.Controls.Add(this.label8);
            this.groupBox1.Controls.Add(this.tbGameBisectTo);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.tbEngineBisectTo);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.tbBisectVariable);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.cbMultiThread);
            this.groupBox1.Controls.Add(this.cmbScripts);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.cbConfigs);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.btnBisect);
            this.groupBox1.Controls.Add(this.tbMap);
            this.groupBox1.Controls.Add(this.lbMap);
            this.groupBox1.Controls.Add(this.tbEngine);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.tbGame);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(650, 295);
            this.groupBox1.TabIndex = 8;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Bisect configuration";
            // 
            // cbBenchmark
            // 
            this.cbBenchmark.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbBenchmark.FormattingEnabled = true;
            this.cbBenchmark.Location = new System.Drawing.Point(114, 19);
            this.cbBenchmark.Name = "cbBenchmark";
            this.cbBenchmark.Size = new System.Drawing.Size(184, 21);
            this.cbBenchmark.TabIndex = 21;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(45, 22);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(63, 13);
            this.label8.TabIndex = 20;
            this.label8.Text = "Test to use:";
            // 
            // tbGameBisectTo
            // 
            this.tbGameBisectTo.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.tbGameBisectTo.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.tbGameBisectTo.Location = new System.Drawing.Point(443, 102);
            this.tbGameBisectTo.Name = "tbGameBisectTo";
            this.tbGameBisectTo.Size = new System.Drawing.Size(184, 20);
            this.tbGameBisectTo.TabIndex = 19;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(363, 105);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(74, 13);
            this.label6.TabIndex = 18;
            this.label6.Text = "Mod bisect to:";
            // 
            // tbEngineBisectTo
            // 
            this.tbEngineBisectTo.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.tbEngineBisectTo.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.tbEngineBisectTo.Location = new System.Drawing.Point(443, 72);
            this.tbEngineBisectTo.Name = "tbEngineBisectTo";
            this.tbEngineBisectTo.Size = new System.Drawing.Size(184, 20);
            this.tbEngineBisectTo.TabIndex = 17;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(351, 75);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(86, 13);
            this.label5.TabIndex = 16;
            this.label5.Text = "Engine bisect to:";
            // 
            // tbBisectVariable
            // 
            this.tbBisectVariable.Location = new System.Drawing.Point(114, 46);
            this.tbBisectVariable.Name = "tbBisectVariable";
            this.tbBisectVariable.Size = new System.Drawing.Size(184, 20);
            this.tbBisectVariable.TabIndex = 15;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(17, 49);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(91, 13);
            this.label2.TabIndex = 14;
            this.label2.Text = "Variable to bisect:";
            // 
            // cbMultiThread
            // 
            this.cbMultiThread.AutoSize = true;
            this.cbMultiThread.Location = new System.Drawing.Point(114, 230);
            this.cbMultiThread.Name = "cbMultiThread";
            this.cbMultiThread.Size = new System.Drawing.Size(128, 17);
            this.cbMultiThread.TabIndex = 13;
            this.cbMultiThread.Text = "Multi threaded engine";
            this.cbMultiThread.UseVisualStyleBackColor = true;
            // 
            // cmbScripts
            // 
            this.cmbScripts.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbScripts.FormattingEnabled = true;
            this.cmbScripts.Location = new System.Drawing.Point(114, 150);
            this.cmbScripts.Name = "cmbScripts";
            this.cmbScripts.Size = new System.Drawing.Size(184, 21);
            this.cmbScripts.TabIndex = 12;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(48, 153);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(60, 13);
            this.label1.TabIndex = 11;
            this.label1.Text = "Start script:";
            // 
            // cbConfigs
            // 
            this.cbConfigs.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbConfigs.FormattingEnabled = true;
            this.cbConfigs.Location = new System.Drawing.Point(114, 177);
            this.cbConfigs.Name = "cbConfigs";
            this.cbConfigs.Size = new System.Drawing.Size(184, 21);
            this.cbConfigs.TabIndex = 10;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(68, 180);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(40, 13);
            this.label7.TabIndex = 9;
            this.label7.Text = "Config:";
            // 
            // btnBisect
            // 
            this.btnBisect.Location = new System.Drawing.Point(114, 266);
            this.btnBisect.Name = "btnBisect";
            this.btnBisect.Size = new System.Drawing.Size(85, 23);
            this.btnBisect.TabIndex = 7;
            this.btnBisect.Text = "Start bisect";
            this.btnBisect.UseVisualStyleBackColor = true;
            this.btnBisect.Click += new System.EventHandler(this.btnAddTest_Click);
            // 
            // tbMap
            // 
            this.tbMap.Location = new System.Drawing.Point(114, 204);
            this.tbMap.Name = "tbMap";
            this.tbMap.Size = new System.Drawing.Size(184, 20);
            this.tbMap.TabIndex = 5;
            // 
            // lbMap
            // 
            this.lbMap.AutoSize = true;
            this.lbMap.Location = new System.Drawing.Point(30, 207);
            this.lbMap.Name = "lbMap";
            this.lbMap.Size = new System.Drawing.Size(78, 13);
            this.lbMap.TabIndex = 4;
            this.lbMap.Text = "Map (override):";
            // 
            // tbEngine
            // 
            this.tbEngine.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.tbEngine.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.tbEngine.Location = new System.Drawing.Point(114, 72);
            this.tbEngine.Name = "tbEngine";
            this.tbEngine.Size = new System.Drawing.Size(184, 20);
            this.tbEngine.TabIndex = 1;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(65, 75);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(43, 13);
            this.label3.TabIndex = 0;
            this.label3.Text = "Engine:";
            // 
            // tbGame
            // 
            this.tbGame.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.tbGame.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.tbGame.Location = new System.Drawing.Point(114, 102);
            this.tbGame.Name = "tbGame";
            this.tbGame.Size = new System.Drawing.Size(184, 20);
            this.tbGame.TabIndex = 3;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(30, 105);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(78, 13);
            this.label4.TabIndex = 2;
            this.label4.Text = "Mod (override):";
            // 
            // tbBisectLog
            // 
            this.tbBisectLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbBisectLog.Location = new System.Drawing.Point(337, 128);
            this.tbBisectLog.Multiline = true;
            this.tbBisectLog.Name = "tbBisectLog";
            this.tbBisectLog.ReadOnly = true;
            this.tbBisectLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbBisectLog.Size = new System.Drawing.Size(290, 119);
            this.tbBisectLog.TabIndex = 22;
            // 
            // BisectForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(696, 322);
            this.Controls.Add(this.groupBox1);
            this.Name = "BisectForm";
            this.Text = "BisectForm";
            this.Load += new System.EventHandler(this.BisectForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox cbMultiThread;
        private System.Windows.Forms.ComboBox cmbScripts;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cbConfigs;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Button btnBisect;
        private System.Windows.Forms.TextBox tbMap;
        private System.Windows.Forms.Label lbMap;
        private System.Windows.Forms.TextBox tbEngine;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox tbGame;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox cbBenchmark;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox tbGameBisectTo;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox tbEngineBisectTo;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox tbBisectVariable;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tbBisectLog;
    }
}