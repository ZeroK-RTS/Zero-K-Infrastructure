using ZeroKLobby.LuaMgr;
using ZeroKLobby.MapDownloader;
using ZeroKLobby.MicroLobby;
using ZeroKLobby.Notifications;

namespace ZeroKLobby
{
  partial class FormMain
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
		this.components = new System.ComponentModel.Container();
		this.systrayIcon = new System.Windows.Forms.NotifyIcon(this.components);
		this.trayStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
		this.btnExit = new System.Windows.Forms.ToolStripMenuItem();
		this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
		this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
		this.notifySection1 = new ZeroKLobby.Notifications.NotifySection();
		this.timer1 = new System.Windows.Forms.Timer(this.components);
		this.elementHost1 = new System.Windows.Forms.Integration.ElementHost();
		this.navigationControl1 = new ZeroKLobby.NavigationControl();
		this.trayStrip.SuspendLayout();
		this.tableLayoutPanel1.SuspendLayout();
		this.SuspendLayout();
		// 
		// systrayIcon
		// 
		this.systrayIcon.ContextMenuStrip = this.trayStrip;
		this.systrayIcon.Text = "Zero-K";
		this.systrayIcon.Visible = true;
		this.systrayIcon.Click += new System.EventHandler(this.systrayIcon_Click);
		this.systrayIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.systrayIcon_MouseDoubleClick);
		this.systrayIcon.MouseDown += new System.Windows.Forms.MouseEventHandler(this.systrayIcon_MouseDown);
		// 
		// trayStrip
		// 
		this.trayStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnExit});
		this.trayStrip.Name = "trayStrip";
		this.trayStrip.Size = new System.Drawing.Size(93, 26);
		this.trayStrip.Opening += new System.ComponentModel.CancelEventHandler(this.trayStrip_Opening);
		// 
		// btnExit
		// 
		this.btnExit.Name = "btnExit";
		this.btnExit.Size = new System.Drawing.Size(92, 22);
		this.btnExit.Text = "Exit";
		this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
		// 
		// toolTip1
		// 
		this.toolTip1.AutomaticDelay = 300;
		// 
		// tableLayoutPanel1
		// 
		this.tableLayoutPanel1.ColumnCount = 1;
		this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
		this.tableLayoutPanel1.Controls.Add(this.elementHost1, 0, 0);
		this.tableLayoutPanel1.Controls.Add(this.notifySection1, 0, 1);
		this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
		this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
		this.tableLayoutPanel1.Name = "tableLayoutPanel1";
		this.tableLayoutPanel1.RowCount = 2;
		this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
		this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
		this.tableLayoutPanel1.Size = new System.Drawing.Size(1116, 670);
		this.tableLayoutPanel1.TabIndex = 6;
		// 
		// notifySection1
		// 
		this.notifySection1.Dock = System.Windows.Forms.DockStyle.Fill;
		this.notifySection1.Location = new System.Drawing.Point(0, 669);
		this.notifySection1.Margin = new System.Windows.Forms.Padding(0);
		this.notifySection1.Name = "notifySection1";
		this.notifySection1.Padding = new System.Windows.Forms.Padding(4);
		this.notifySection1.Size = new System.Drawing.Size(1116, 1);
		this.notifySection1.TabIndex = 6;
		// 
		// timer1
		// 
		this.timer1.Interval = 250;
		this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
		// 
		// elementHost1
		// 
		this.elementHost1.Dock = System.Windows.Forms.DockStyle.Fill;
		this.elementHost1.Location = new System.Drawing.Point(3, 3);
		this.elementHost1.Name = "elementHost1";
		this.elementHost1.Size = new System.Drawing.Size(1110, 663);
		this.elementHost1.TabIndex = 7;
		this.elementHost1.Text = "elementHost1";
		this.elementHost1.Child = this.navigationControl1;
		// 
		// FormMain
		// 
		this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
		this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.ClientSize = new System.Drawing.Size(1116, 670);
		this.Controls.Add(this.tableLayoutPanel1);
		this.MinimumSize = new System.Drawing.Size(550, 300);
		this.Name = "FormMain";
		this.Text = "Zero-K lobby";
		this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormMain_FormClosing);
		this.Load += new System.EventHandler(this.MainForm_Load);
		this.SizeChanged += new System.EventHandler(this.FormMain_SizeChanged);
		this.trayStrip.ResumeLayout(false);
		this.tableLayoutPanel1.ResumeLayout(false);
		this.ResumeLayout(false);

    }

    #endregion

	private System.Windows.Forms.ToolTip toolTip1;
    private System.Windows.Forms.ContextMenuStrip trayStrip;
		private System.Windows.Forms.ToolStripMenuItem btnExit;
		private System.Windows.Forms.NotifyIcon systrayIcon;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private NotifySection notifySection1;
		private System.Windows.Forms.Timer timer1;
		private System.Windows.Forms.Integration.ElementHost elementHost1;
		private NavigationControl navigationControl1;
  }
}