namespace ZeroKLobby.MapDownloader
{
  partial class FormMapPicker
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
      if (disposing && (components != null)) {
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
			this.listBox1 = new System.Windows.Forms.ListBox();
			this.label1 = new System.Windows.Forms.Label();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.btnOk = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.rbMaps = new System.Windows.Forms.RadioButton();
			this.rbMods = new System.Windows.Forms.RadioButton();
			this.btnAll = new System.Windows.Forms.Button();
			this.progressBar1 = new System.Windows.Forms.ProgressBar();
			this.btnReload = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// listBox1
			// 
			this.listBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
									| System.Windows.Forms.AnchorStyles.Left)
									| System.Windows.Forms.AnchorStyles.Right)));
			this.listBox1.FormattingEnabled = true;
			this.listBox1.HorizontalScrollbar = true;
			this.listBox1.Location = new System.Drawing.Point(12, 65);
			this.listBox1.Name = "listBox1";
			this.listBox1.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
			this.listBox1.Size = new System.Drawing.Size(288, 173);
			this.listBox1.TabIndex = 4;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(13, 13);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(77, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Filter selection:";
			// 
			// textBox1
			// 
			this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
									| System.Windows.Forms.AnchorStyles.Right)));
			this.textBox1.Location = new System.Drawing.Point(96, 10);
			this.textBox1.Name = "textBox1";
			this.textBox1.Size = new System.Drawing.Size(204, 20);
			this.textBox1.TabIndex = 1;
			this.textBox1.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
			// 
			// btnOk
			// 
			this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnOk.Location = new System.Drawing.Point(28, 254);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(75, 23);
			this.btnOk.TabIndex = 5;
			this.btnOk.Text = "OK";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(214, 254);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(75, 23);
			this.btnCancel.TabIndex = 6;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			// 
			// rbMaps
			// 
			this.rbMaps.AutoSize = true;
			this.rbMaps.Checked = true;
			this.rbMaps.Location = new System.Drawing.Point(12, 36);
			this.rbMaps.Name = "rbMaps";
			this.rbMaps.Size = new System.Drawing.Size(51, 17);
			this.rbMaps.TabIndex = 2;
			this.rbMaps.TabStop = true;
			this.rbMaps.Text = "Maps";
			this.rbMaps.UseVisualStyleBackColor = true;
			this.rbMaps.CheckedChanged += new System.EventHandler(this.rbMaps_CheckedChanged);
			// 
			// rbMods
			// 
			this.rbMods.AutoSize = true;
			this.rbMods.Location = new System.Drawing.Point(69, 36);
			this.rbMods.Name = "rbMods";
			this.rbMods.Size = new System.Drawing.Size(51, 17);
			this.rbMods.TabIndex = 3;
			this.rbMods.Text = "Mods";
			this.rbMods.UseVisualStyleBackColor = true;
			this.rbMods.CheckedChanged += new System.EventHandler(this.rbMods_CheckedChanged);
			// 
			// btnAll
			// 
			this.btnAll.Location = new System.Drawing.Point(114, 254);
			this.btnAll.Name = "btnAll";
			this.btnAll.Size = new System.Drawing.Size(75, 23);
			this.btnAll.TabIndex = 7;
			this.btnAll.Text = "All";
			this.btnAll.UseVisualStyleBackColor = true;
			this.btnAll.Visible = false;
			this.btnAll.Click += new System.EventHandler(this.btnAll_Click);
			// 
			// progressBar1
			// 
			this.progressBar1.Location = new System.Drawing.Point(218, 46);
			this.progressBar1.Name = "progressBar1";
			this.progressBar1.Size = new System.Drawing.Size(82, 13);
			this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
			this.progressBar1.TabIndex = 8;
			// 
			// btnReload
			// 
			this.btnReload.Location = new System.Drawing.Point(163, 36);
			this.btnReload.Name = "btnReload";
			this.btnReload.Size = new System.Drawing.Size(49, 23);
			this.btnReload.TabIndex = 9;
			this.btnReload.Text = "Reload";
			this.btnReload.UseVisualStyleBackColor = true;
			this.btnReload.Click += new System.EventHandler(this.btnReload_Click);
			// 
			// FormMapPicker
			// 
			this.AcceptButton = this.btnOk;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(312, 289);
			this.Controls.Add(this.btnReload);
			this.Controls.Add(this.progressBar1);
			this.Controls.Add(this.btnAll);
			this.Controls.Add(this.rbMods);
			this.Controls.Add(this.rbMaps);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOk);
			this.Controls.Add(this.textBox1);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.listBox1);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(320, 316);
			this.Name = "FormMapPicker";
			this.Text = "Select files for download";
			this.Load += new System.EventHandler(this.FormMapPicker_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.ListBox listBox1;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.TextBox textBox1;
    private System.Windows.Forms.Button btnOk;
    private System.Windows.Forms.Button btnCancel;
    private System.Windows.Forms.RadioButton rbMaps;
    private System.Windows.Forms.RadioButton rbMods;
		private System.Windows.Forms.Button btnAll;
		private System.Windows.Forms.ProgressBar progressBar1;
		private System.Windows.Forms.Button btnReload;
  }
}