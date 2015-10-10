using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ZkData;

namespace ZeroKLobby.MicroLobby
{
	public partial class SettingsTab: UserControl, INavigatable
	{
		readonly ContextMenu cmDisplay;

		public SettingsTab()
		{
			InitializeComponent();

			helpButton.MouseUp += helpButton_MouseUp;

			var isDesigner = Process.GetCurrentProcess().ProcessName == "devenv"; // workaround for this.DesignMode not working
			if (isDesigner) return;

			var cfRoot = Program.SpringPaths.WritableDirectory;

			cmDisplay = new ContextMenu();
			
            cmDisplay.MenuItems.Add(new MenuItem("Edit engine settings (manually)", (o, x) => Utils.SafeStart(Program.SpringPaths.GetSpringConfigPath())));
			cmDisplay.MenuItems.Add(new MenuItem("Edit LUPS settings", (o, x) => Utils.SafeStart(Utils.MakePath(cfRoot, "lups.cfg"))));
			cmDisplay.MenuItems.Add(new MenuItem("Edit cmdcolors", (o, x) => Utils.SafeStart(Utils.MakePath(cfRoot, "cmdcolors.txt"))));
            cmDisplay.MenuItems.Add(new MenuItem("Edit keybinds", (o, x) => Utils.SafeStart(Utils.MakePath(cfRoot, "LuaUI", "Configs", "zk_keys.lua"))));

            Program.ToolTip.SetText(cbSafeMode, "Turns off many things that are known to cause problems (on PC/Mac's with lower-end graphic cards). Use if the game is crashing.\nWill override Springsetting.cfg");
            Program.ToolTip.SetText(cbHwCursor,"HW cursor is uneffected by ingame lag, but it can become invisible on some machines");
            
            Program.ToolTip.SetText(cbWindowed, "Windowed: Run game on desktop in a window\nBorderless: Run game on desktop in a borderless window\nFullscreen: Run game fullscreen");
            Program.ToolTip.SetText(button5, "Springsettings.cfg and Lups.cfg tuned for performance and compatibility; many graphical features are disabled");
            Program.ToolTip.SetText(button1, "Springsettings.cfg and Lups.cfg for performance; some graphical features are disabled");
            Program.ToolTip.SetText(button2, "Springsettings.cfg and Lups.cfg recommended for medium settings");
            Program.ToolTip.SetText(button3, "Springsettings.cfg and Lups.cfg recommended for high settings");
            Program.ToolTip.SetText(button4, "Springsettings.cfg and Lups.cfg recommended for Ultra settings");
            Program.ToolTip.SetText(btnCustom, "Edit current Springsettings.cfg");
            Program.ToolTip.SetText(btnRapid, "Monitor certain mods for latest version and auto-download them when available.");
            Program.ToolTip.SetText(developmentButton, "Go to Zero-K development page.");
            Program.ToolTip.SetText(lobbyLogButton, "Diagnostic log for ZKL lobby client ( Useful to report things such as: download issue or lobby issue)");
            Program.ToolTip.SetText(gameLogButton, "Diagnostic log for Spring engine (Useful to report things such as: ingame graphic bug or game crash)");
            Program.ToolTip.SetText(btnDefaults, "Local data reset?");
            Program.ToolTip.SetText(btnOfflineSkirmish, "Create custom offline game versus AI");
		}


        bool refreshingConfig = false;
		public void RefreshConfig()
		{
            refreshingConfig = true;
			propertyGrid1.SelectedObject = Program.Conf;
            if (Program.EngineConfigurator.GetConfigValue("Fullscreen") == "0" && Program.EngineConfigurator.GetConfigValue("WindowBorderless") == "1")
                cbWindowed.SelectedItem = "Borderless";
            else if(Program.EngineConfigurator.GetConfigValue("Fullscreen") == "0")
                cbWindowed.SelectedItem = "Windowed";
            else 
                cbWindowed.SelectedItem = "Fullscreen";

            cbHwCursor.Checked = Program.EngineConfigurator.GetConfigValue("HardwareCursor") == "1";
            tbResx.Text = Program.EngineConfigurator.GetConfigValue("XResolution");
            tbResy.Text = Program.EngineConfigurator.GetConfigValue("YResolution");
            refreshingConfig = false;
            cbSafeMode.Checked = Program.Conf.UseSafeMode;
		}


	    public void SaveConfig() {
            if ((string)cbWindowed.SelectedItem == "Fullscreen")
            {
                Program.EngineConfigurator.SetConfigValue("Fullscreen", "1");
            }
            else if ((string)cbWindowed.SelectedItem == "Borderless")
            {
                Program.EngineConfigurator.SetConfigValue("Fullscreen", "0");
                Program.EngineConfigurator.SetConfigValue("WindowBorderless", "1");
            }
            else if ((string)cbWindowed.SelectedItem == "Windowed")
            {
                Program.EngineConfigurator.SetConfigValue("Fullscreen", "0");
                Program.EngineConfigurator.SetConfigValue("WindowBorderless", "0");
            }

	        int resX;
	        int.TryParse(tbResx.Text, out resX);
	        int resY;
	        int.TryParse(tbResy.Text, out resY);

            Program.EngineConfigurator.SetConfigValue("XResolution", resX.ToString());
            Program.EngineConfigurator.SetConfigValue("YResolution", resY.ToString());
	        Program.EngineConfigurator.SetConfigValue("HardwareCursor", cbHwCursor.Checked?"1":"0");
            Program.EngineConfigurator.SetConfigValue("WindowState", "0"); // neded for borderless
            Program.EngineConfigurator.SetConfigValue("WindowPosY", "0"); // neded for borderless
            Program.EngineConfigurator.SetConfigValue("WindowPosX", "0"); // neded for borderless
            Program.Conf.UseSafeMode = cbSafeMode.Checked;
	    }

	    public string PathHead { get { return "settings"; } }

        public bool TryNavigate(params string[] path)
		{
			return path.Length > 0 && path[0] == PathHead;
		}

		public bool Hilite(HiliteLevel level, string path)
		{
			return false;
		}

		public string GetTooltip(params string[] path)
		{
			return null;
		}

	    public void Reload() {
	        
	    }

	    public bool CanReload { get { return false; }}

        public bool IsBusy { get { return false; } }

	    void SettingsTab_Load(object sender, EventArgs e)
		{
			RefreshConfig();
            //make sure the split start at 203 (relative to any DPI scale)
            DpiMeasurement.DpiXYMeasurement(this);
            int splitDistance = DpiMeasurement.ScaleValueY(203);//DpiMeasurement is a static class stored in ZeroKLobby\Util.cs
            splitContainerAtMid.SplitterDistance = splitDistance;
		}

		void btnBrowse_Click(object sender, EventArgs e)
		{
			Utils.SafeStart(Program.SpringPaths.WritableDirectory);
		}

		void btnDisplay_Click(object sender, EventArgs e)
		{
			cmDisplay.Show(this, PointToClient(MousePosition));
		}


		void button1_Click(object sender, EventArgs e)
		{
			Program.EngineConfigurator.Configure(true, 1);
            RefreshConfig();
		}

		void button2_Click(object sender, EventArgs e)
		{
			Program.EngineConfigurator.Configure(true, 2);
            RefreshConfig();
		}

		void button3_Click(object sender, EventArgs e)
		{
			Program.EngineConfigurator.Configure(true, 3);
            RefreshConfig();
		}

		void button4_Click(object sender, EventArgs e)
		{
			Program.EngineConfigurator.Configure(true, 4);
            RefreshConfig();
		}

		void button5_Click(object sender, EventArgs e)
		{
			Program.EngineConfigurator.Configure(true, 0);
            RefreshConfig();
		}



		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		void helpButton_MouseUp(object sender, MouseEventArgs ea)
		{
			var menu = new ContextMenu();
			var joinItem = new MenuItem("Ask in the ZK channel (#zk)");
			joinItem.Click += (s, e) => NavigationControl.Instance.Path = "chat/channel/zk";
			menu.MenuItems.Add(joinItem);
			var helpForumItem = new MenuItem("Ask in the Help Forum");
			helpForumItem.Click += helpForumItem_Click;
			menu.MenuItems.Add(helpForumItem);
			var adminsItem = new MenuItem("Ask an Administrator");
			foreach (var admin in Program.TasClient.ExistingUsers.Values.Where(u => (u.IsAdmin)&& !u.IsBot).OrderBy(u => u.IsAway ? 1 : 0))
			{
				var item = new MenuItem(admin.Name + (admin.IsAway ? " (Idle)" : String.Empty));
				var adminName = admin.Name;
				item.Click += (s, e) => NavigationControl.Instance.Path = "chat/user/" + adminName;
				adminsItem.MenuItems.Add(item);
			}
			menu.MenuItems.Add(adminsItem);
			menu.Show(helpButton, ea.Location);
		}

		void helpForumItem_Click(object sender, EventArgs e)
		{
            Program.MainWindow.navigationControl.Path = string.Format("{0}/Forum?categoryID=3", GlobalConst.BaseSiteUrl); //open using Navigation Bar. If internal browser fail, it open external browser.
		}

		void lobbyLogButton_Click(object sender, EventArgs e)
		{
			ActionHandler.ShowLog();
		}

        private void gameLogButton_Click(object sender, EventArgs e)
        {
            Utils.SafeStart(Utils.MakePath (Program.SpringPaths.WritableDirectory, "infolog.txt"));
        }

		void developmentButton_Click(object sender, EventArgs e)
		{
            //try
            //{
            //    Process.Start("https://github.com/ZeroK-RTS");
            //}
            //catch {}
            Program.MainWindow.navigationControl.Path = "https://github.com/ZeroK-RTS";
		}

		void siteFeatureRequestItem_Click(object sender, EventArgs e)
		{
            //try
            //{
            //    Process.Start("http://code.google.com/p/zero-k/issues/entry?template=Feature%20Request");
            //}
            //catch {}
            Program.MainWindow.navigationControl.Path = "http://code.google.com/p/zero-k/issues/entry?template=Feature%20Request";
		}

        private void settingsControlChanged(object sender, EventArgs e)
        {
            if (!refreshingConfig) SaveConfig();
        }


        private void btnRapid_Click(object sender, EventArgs e)
        {
            Program.MainWindow.navigationControl.Path = "rapid";
        }

        private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {

            btnRestart.Visible = true;

        }


        private void btnRestart_Click(object sender, EventArgs e)
        {
            Program.Restart();
        }

        private void btnDefaults_Click(object sender, EventArgs e)
        {
            MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button2;
            if (MessageBox.Show("Do you want to reset configuration to defaults and delete all cached content?", "Local data reset", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, defaultButton) == DialogResult.Yes)
            {
                Program.EngineConfigurator.Reset();
                var path = Program.SpringPaths.WritableDirectory;
                Directory.Delete(Utils.MakePath(path,"cache"),true);
                Directory.Delete(Utils.MakePath(path, "pool"), true);
                Directory.Delete(Utils.MakePath(path, "packages"), true);
                Directory.Delete(Utils.MakePath(path, "LuaUI"), true);
                Directory.Delete(Utils.MakePath(path, "temp"), true);

                Program.Restart();
            }
        }

        private void cbSafeMode_CheckedChanged(object sender, EventArgs e)
        {
            Program.Conf.UseSafeMode = cbSafeMode.Checked;
            Program.SpringPaths.SafeMode = cbSafeMode.Checked;
            Program.SaveConfig();
        }

        private void btnBenchmarker_Click(object sender, EventArgs e) {
            var benchmarker = new Benchmarker.MainForm(Program.SpringPaths, Program.SpringScanner, Program.Downloader);
            benchmarker.Show();
        }

        private void btnCustom_Click(object sender, EventArgs e)
        {
            ActionHandler.ShowSpringsetting();
        }

        private void panel1_Resize(object sender, EventArgs e)
        {
            if (Program.MainWindow!=null && Program.MainWindow.WindowState == FormWindowState.Minimized) return;
			//prevent splitter from being dragged when window resize
            DpiMeasurement.DpiXYMeasurement(this); //this measurement use cached value. It won't cost anything if another measurement was already done in other control element
            int splitDistance = DpiMeasurement.ScaleValueY(203);//DpiMeasurement is a static class stored in ZeroKLobby\Util.cs
            splitDistance = Math.Min(splitDistance, splitContainerAtMid.Width - splitContainerAtMid.Panel2MinSize);
            splitContainerAtMid.SplitterDistance = splitDistance; //must obey minimum size constraint
        }

        private void btnOfflineSkirmish_Click(object sender, EventArgs e)
        {
            Program.MainWindow.navigationControl.Path = "zk://extra/skirmish";
        }
	}
}