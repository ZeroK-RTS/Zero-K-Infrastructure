namespace ZeroKLobby.MapDownloader
{
  partial class DownloaderTab
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

    #region Component Designer generated code

    /// <summary> 
    /// Required method for Designer support - do not modify 
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      this.components = new System.ComponentModel.Container();
      this.groupBox1 = new System.Windows.Forms.GroupBox();
      this.btnReload = new System.Windows.Forms.Button();
      this.tvAvailable = new System.Windows.Forms.TreeView();
      this.panel1 = new System.Windows.Forms.Panel();
      this.label2 = new System.Windows.Forms.Label();
      this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
      this.groupBox2 = new System.Windows.Forms.GroupBox();
      this.lbInstalled = new System.Windows.Forms.ListBox();
      this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
      this.panel2 = new System.Windows.Forms.Panel();
      this.groupBox1.SuspendLayout();
      this.panel1.SuspendLayout();
      this.tableLayoutPanel1.SuspendLayout();
      this.groupBox2.SuspendLayout();
      this.panel2.SuspendLayout();
      this.SuspendLayout();
      // 
      // groupBox1
      // 
      this.groupBox1.Controls.Add(this.btnReload);
      this.groupBox1.Controls.Add(this.tvAvailable);
      this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
      this.groupBox1.Location = new System.Drawing.Point(3, 3);
      this.groupBox1.Name = "groupBox1";
      this.groupBox1.Size = new System.Drawing.Size(197, 418);
      this.groupBox1.TabIndex = 0;
      this.groupBox1.TabStop = false;
      this.groupBox1.Text = "Available packages";
      // 
      // btnReload
      // 
      this.btnReload.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.btnReload.Location = new System.Drawing.Point(6, 391);
      this.btnReload.Name = "btnReload";
      this.btnReload.Size = new System.Drawing.Size(75, 23);
      this.btnReload.TabIndex = 1;
      this.btnReload.Text = "Reload";
      this.toolTip1.SetToolTip(this.btnReload, "Reloads available packages");
      this.btnReload.UseVisualStyleBackColor = true;
      this.btnReload.Click += new System.EventHandler(this.btnReload_Click);
      // 
      // tvAvailable
      // 
      this.tvAvailable.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.tvAvailable.Location = new System.Drawing.Point(3, 16);
      this.tvAvailable.Name = "tvAvailable";
      this.tvAvailable.Size = new System.Drawing.Size(191, 369);
      this.tvAvailable.TabIndex = 0;
      this.tvAvailable.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.tvAvailable_NodeMouseDoubleClick);
      // 
      // panel1
      // 
      this.panel1.Controls.Add(this.label2);
      this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
      this.panel1.Location = new System.Drawing.Point(206, 3);
      this.panel1.Name = "panel1";
      this.panel1.Size = new System.Drawing.Size(119, 418);
      this.panel1.TabIndex = 2;
      // 
      // label2
      // 
      this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.label2.Location = new System.Drawing.Point(3, 28);
      this.label2.Name = "label2";
      this.label2.Size = new System.Drawing.Size(113, 145);
      this.label2.TabIndex = 0;
      this.label2.Text = "Double click to install or remove package.\r\n\r\nSelected packages are automatically" +
          " kept updated and content is shared between them.\r\n\r\n";
      // 
      // tableLayoutPanel1
      // 
      this.tableLayoutPanel1.ColumnCount = 3;
      this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 61.84615F));
      this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 38.15385F));
      this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 199F));
      this.tableLayoutPanel1.Controls.Add(this.groupBox1, 0, 0);
      this.tableLayoutPanel1.Controls.Add(this.groupBox2, 2, 0);
      this.tableLayoutPanel1.Controls.Add(this.panel1, 1, 0);
      this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
      this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
      this.tableLayoutPanel1.Name = "tableLayoutPanel1";
      this.tableLayoutPanel1.RowCount = 1;
      this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
      this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
      this.tableLayoutPanel1.Size = new System.Drawing.Size(528, 424);
      this.tableLayoutPanel1.TabIndex = 15;
      // 
      // groupBox2
      // 
      this.groupBox2.Controls.Add(this.lbInstalled);
      this.groupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
      this.groupBox2.Location = new System.Drawing.Point(331, 3);
      this.groupBox2.Name = "groupBox2";
      this.groupBox2.Size = new System.Drawing.Size(194, 418);
      this.groupBox2.TabIndex = 1;
      this.groupBox2.TabStop = false;
      this.groupBox2.Text = "Selected packages";
      // 
      // lbInstalled
      // 
      this.lbInstalled.Dock = System.Windows.Forms.DockStyle.Fill;
      this.lbInstalled.FormattingEnabled = true;
      this.lbInstalled.Location = new System.Drawing.Point(3, 16);
      this.lbInstalled.Name = "lbInstalled";
      this.lbInstalled.Size = new System.Drawing.Size(188, 399);
      this.lbInstalled.TabIndex = 0;
      this.lbInstalled.DoubleClick += new System.EventHandler(this.lbInstalled_DoubleClick);
      // 
      // panel2
      // 
      this.panel2.Controls.Add(this.tableLayoutPanel1);
      this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
      this.panel2.Location = new System.Drawing.Point(0, 0);
      this.panel2.Name = "panel2";
      this.panel2.Size = new System.Drawing.Size(528, 424);
      this.panel2.TabIndex = 17;
      // 
      // DownloaderTab
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.Controls.Add(this.panel2);
      this.Name = "DownloaderTab";
      this.Size = new System.Drawing.Size(528, 424);
      this.groupBox1.ResumeLayout(false);
      this.panel1.ResumeLayout(false);
      this.tableLayoutPanel1.ResumeLayout(false);
      this.groupBox2.ResumeLayout(false);
      this.panel2.ResumeLayout(false);
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Button btnReload;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.TreeView tvAvailable;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.GroupBox groupBox2;
    private System.Windows.Forms.ListBox lbInstalled;
		private System.Windows.Forms.Panel panel2;
  }
}