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
        this.tabControl = new System.Windows.Forms.TabControl();
        this.tabPageGames = new System.Windows.Forms.TabPage();
        this.tabPageBattles = new System.Windows.Forms.TabPage();
        this.tabPageChat = new System.Windows.Forms.TabPage();
        this.tabPageWidgets = new System.Windows.Forms.TabPage();
        this.tabPageDownloader = new System.Windows.Forms.TabPage();
        this.tabPageSettings = new System.Windows.Forms.TabPage();
        this.tabPageHelp = new System.Windows.Forms.TabPage();
        this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
        this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
        this.timer1 = new System.Windows.Forms.Timer(this.components);
        this.tabPageServer = new System.Windows.Forms.TabPage();
        this.startPage1 = new StartPage();
        this.battleListContainer1 = new BattleListTab();
        this.chatControl1 = new ChatTab();
        this.luaMgrTab1 = new LuaMgrTab();
        this.downloaderTab1 = new DownloaderTab();
        this.settingsTab1 = new SettingsTab();
        this.helpControl1 = new HelpControl();
        this.notifySection1 = new NotifySection();
        this.serverTab1 = new ServerTab();
        this.trayStrip.SuspendLayout();
        this.tabControl.SuspendLayout();
        this.tabPageGames.SuspendLayout();
        this.tabPageBattles.SuspendLayout();
        this.tabPageChat.SuspendLayout();
        this.tabPageWidgets.SuspendLayout();
        this.tabPageDownloader.SuspendLayout();
        this.tabPageSettings.SuspendLayout();
        this.tabPageHelp.SuspendLayout();
        this.tableLayoutPanel1.SuspendLayout();
        this.tabPageServer.SuspendLayout();
        this.SuspendLayout();
        // 
        // systrayIcon
        // 
        this.systrayIcon.ContextMenuStrip = this.trayStrip;
        this.systrayIcon.Text = "Spring Downloader";
        this.systrayIcon.Visible = true;
        this.systrayIcon.Click += new System.EventHandler(this.systrayIcon_Click);
        this.systrayIcon.MouseDown += new System.Windows.Forms.MouseEventHandler(this.systrayIcon_MouseDown);
        this.systrayIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.systrayIcon_MouseDoubleClick);
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
        // tabControl
        // 
        this.tabControl.Controls.Add(this.tabPageGames);
        this.tabControl.Controls.Add(this.tabPageBattles);
        this.tabControl.Controls.Add(this.tabPageChat);
        this.tabControl.Controls.Add(this.tabPageWidgets);
        this.tabControl.Controls.Add(this.tabPageDownloader);
        this.tabControl.Controls.Add(this.tabPageSettings);
        this.tabControl.Controls.Add(this.tabPageHelp);
        this.tabControl.Controls.Add(this.tabPageServer);
        this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
        this.tabControl.Location = new System.Drawing.Point(3, 3);
        this.tabControl.Name = "tabControl";
        this.tabControl.SelectedIndex = 0;
        this.tabControl.Size = new System.Drawing.Size(1110, 663);
        this.tabControl.TabIndex = 5;
        this.tabControl.SelectedIndexChanged += new System.EventHandler(this.tabControl_SelectedIndexChanged);
        // 
        // tabPageGames
        // 
        this.tabPageGames.Controls.Add(this.startPage1);
        this.tabPageGames.Location = new System.Drawing.Point(4, 22);
        this.tabPageGames.Name = "tabPageGames";
        this.tabPageGames.Padding = new System.Windows.Forms.Padding(3);
        this.tabPageGames.Size = new System.Drawing.Size(1102, 637);
        this.tabPageGames.TabIndex = 1;
        this.tabPageGames.Text = "Start";
        this.tabPageGames.UseVisualStyleBackColor = true;
        // 
        // tabPageBattles
        // 
        this.tabPageBattles.Controls.Add(this.battleListContainer1);
        this.tabPageBattles.Location = new System.Drawing.Point(4, 22);
        this.tabPageBattles.Name = "tabPageBattles";
        this.tabPageBattles.Padding = new System.Windows.Forms.Padding(3);
        this.tabPageBattles.Size = new System.Drawing.Size(1102, 637);
        this.tabPageBattles.TabIndex = 5;
        this.tabPageBattles.Text = "Battles";
        this.tabPageBattles.UseVisualStyleBackColor = true;
        // 
        // tabPageChat
        // 
        this.tabPageChat.Controls.Add(this.chatControl1);
        this.tabPageChat.Location = new System.Drawing.Point(4, 22);
        this.tabPageChat.Name = "tabPageChat";
        this.tabPageChat.Padding = new System.Windows.Forms.Padding(3);
        this.tabPageChat.Size = new System.Drawing.Size(1102, 637);
        this.tabPageChat.TabIndex = 4;
        this.tabPageChat.Text = "Chat";
        this.tabPageChat.UseVisualStyleBackColor = true;
        // 
        // tabPageWidgets
        // 
        this.tabPageWidgets.Controls.Add(this.luaMgrTab1);
        this.tabPageWidgets.Location = new System.Drawing.Point(4, 22);
        this.tabPageWidgets.Name = "tabPageWidgets";
        this.tabPageWidgets.Padding = new System.Windows.Forms.Padding(3);
        this.tabPageWidgets.Size = new System.Drawing.Size(1102, 637);
        this.tabPageWidgets.TabIndex = 3;
        this.tabPageWidgets.Text = "Widgets";
        this.tabPageWidgets.UseVisualStyleBackColor = true;
        // 
        // tabPageDownloader
        // 
        this.tabPageDownloader.Controls.Add(this.downloaderTab1);
        this.tabPageDownloader.Location = new System.Drawing.Point(4, 22);
        this.tabPageDownloader.Name = "tabPageDownloader";
        this.tabPageDownloader.Padding = new System.Windows.Forms.Padding(3);
        this.tabPageDownloader.Size = new System.Drawing.Size(1102, 637);
        this.tabPageDownloader.TabIndex = 2;
        this.tabPageDownloader.Text = "Downloader";
        this.tabPageDownloader.UseVisualStyleBackColor = true;
        // 
        // tabPageSettings
        // 
        this.tabPageSettings.Controls.Add(this.settingsTab1);
        this.tabPageSettings.Location = new System.Drawing.Point(4, 22);
        this.tabPageSettings.Name = "tabPageSettings";
        this.tabPageSettings.Padding = new System.Windows.Forms.Padding(3);
        this.tabPageSettings.Size = new System.Drawing.Size(1102, 637);
        this.tabPageSettings.TabIndex = 7;
        this.tabPageSettings.Text = "Settings";
        this.tabPageSettings.UseVisualStyleBackColor = true;
        // 
        // tabPageHelp
        // 
        this.tabPageHelp.Controls.Add(this.helpControl1);
        this.tabPageHelp.Location = new System.Drawing.Point(4, 22);
        this.tabPageHelp.Name = "tabPageHelp";
        this.tabPageHelp.Padding = new System.Windows.Forms.Padding(3);
        this.tabPageHelp.Size = new System.Drawing.Size(1102, 637);
        this.tabPageHelp.TabIndex = 6;
        this.tabPageHelp.Text = "Help";
        this.tabPageHelp.UseVisualStyleBackColor = true;
        // 
        // toolTip1
        // 
        this.toolTip1.AutomaticDelay = 300;
        // 
        // tableLayoutPanel1
        // 
        this.tableLayoutPanel1.ColumnCount = 1;
        this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this.tableLayoutPanel1.Controls.Add(this.tabControl, 0, 0);
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
        // timer1
        // 
        this.timer1.Interval = 250;
        this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
        // 
        // tabPageServer
        // 
        this.tabPageServer.Controls.Add(this.serverTab1);
        this.tabPageServer.Location = new System.Drawing.Point(4, 22);
        this.tabPageServer.Name = "tabPageServer";
        this.tabPageServer.Size = new System.Drawing.Size(1102, 637);
        this.tabPageServer.TabIndex = 8;
        this.tabPageServer.Text = "Server";
        this.tabPageServer.UseVisualStyleBackColor = true;
        // 
        // startPage1
        // 
        this.startPage1.Dock = System.Windows.Forms.DockStyle.Fill;
        this.startPage1.Location = new System.Drawing.Point(3, 3);
        this.startPage1.Name = "startPage1";
        this.startPage1.Size = new System.Drawing.Size(1096, 631);
        this.startPage1.TabIndex = 0;
        // 
        // battleListContainer1
        // 
        this.battleListContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
        this.battleListContainer1.Location = new System.Drawing.Point(3, 3);
        this.battleListContainer1.Name = "battleListContainer1";
        this.battleListContainer1.Size = new System.Drawing.Size(1096, 631);
        this.battleListContainer1.TabIndex = 0;
        // 
        // chatControl1
        // 
        this.chatControl1.Dock = System.Windows.Forms.DockStyle.Fill;
        this.chatControl1.Location = new System.Drawing.Point(3, 3);
        this.chatControl1.Name = "chatControl1";
        this.chatControl1.Size = new System.Drawing.Size(1096, 631);
        this.chatControl1.TabIndex = 0;
        // 
        // luaMgrTab1
        // 
        this.luaMgrTab1.Dock = System.Windows.Forms.DockStyle.Fill;
        this.luaMgrTab1.Enabled = false;
        this.luaMgrTab1.Location = new System.Drawing.Point(3, 3);
        this.luaMgrTab1.Name = "luaMgrTab1";
        this.luaMgrTab1.Size = new System.Drawing.Size(1096, 631);
        this.luaMgrTab1.TabIndex = 0;
        // 
        // downloaderTab1
        // 
        this.downloaderTab1.Dock = System.Windows.Forms.DockStyle.Fill;
        this.downloaderTab1.Location = new System.Drawing.Point(3, 3);
        this.downloaderTab1.Name = "downloaderTab1";
        this.downloaderTab1.Size = new System.Drawing.Size(1096, 631);
        this.downloaderTab1.TabIndex = 0;
        // 
        // settingsTab1
        // 
        this.settingsTab1.Dock = System.Windows.Forms.DockStyle.Fill;
        this.settingsTab1.Location = new System.Drawing.Point(3, 3);
        this.settingsTab1.Name = "settingsTab1";
        this.settingsTab1.Size = new System.Drawing.Size(1096, 631);
        this.settingsTab1.TabIndex = 0;
        // 
        // helpControl1
        // 
        this.helpControl1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        this.helpControl1.Dock = System.Windows.Forms.DockStyle.Fill;
        this.helpControl1.Location = new System.Drawing.Point(3, 3);
        this.helpControl1.Name = "helpControl1";
        this.helpControl1.Size = new System.Drawing.Size(1096, 631);
        this.helpControl1.TabIndex = 0;
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
        // serverTab1
        // 
        this.serverTab1.Dock = System.Windows.Forms.DockStyle.Fill;
        this.serverTab1.Location = new System.Drawing.Point(0, 0);
        this.serverTab1.Name = "serverTab1";
        this.serverTab1.Size = new System.Drawing.Size(1102, 637);
        this.serverTab1.TabIndex = 0;
        // 
        // FormMain
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(1116, 670);
        this.Controls.Add(this.tableLayoutPanel1);
        this.MinimumSize = new System.Drawing.Size(550, 300);
        this.Name = "FormMain";
        this.Text = "Spring Downloader";
        this.Load += new System.EventHandler(this.MainForm_Load);
        this.SizeChanged += new System.EventHandler(this.FormMain_SizeChanged);
        this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormMain_FormClosing);
        this.trayStrip.ResumeLayout(false);
        this.tabControl.ResumeLayout(false);
        this.tabPageGames.ResumeLayout(false);
        this.tabPageBattles.ResumeLayout(false);
        this.tabPageChat.ResumeLayout(false);
        this.tabPageWidgets.ResumeLayout(false);
        this.tabPageDownloader.ResumeLayout(false);
        this.tabPageSettings.ResumeLayout(false);
        this.tabPageHelp.ResumeLayout(false);
        this.tableLayoutPanel1.ResumeLayout(false);
        this.tabPageServer.ResumeLayout(false);
        this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.TabControl tabControl;
    private System.Windows.Forms.ToolTip toolTip1;
    private System.Windows.Forms.TabPage tabPageGames;
    private System.Windows.Forms.ContextMenuStrip trayStrip;
		private System.Windows.Forms.ToolStripMenuItem btnExit;
		private System.Windows.Forms.NotifyIcon systrayIcon;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private NotifySection notifySection1;
		private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.TabPage tabPageDownloader;
        private System.Windows.Forms.TabPage tabPageWidgets;
        private System.Windows.Forms.TabPage tabPageChat;
        private System.Windows.Forms.TabPage tabPageBattles;
        private DownloaderTab downloaderTab1;
        private LuaMgrTab luaMgrTab1;
        private ChatTab chatControl1;
        private BattleListTab battleListContainer1;
        private System.Windows.Forms.TabPage tabPageHelp;
        private HelpControl helpControl1;
        private System.Windows.Forms.TabPage tabPageSettings;
        private StartPage startPage1;
        private SettingsTab settingsTab1;
        private System.Windows.Forms.TabPage tabPageServer;
        private ServerTab serverTab1;
  }
}