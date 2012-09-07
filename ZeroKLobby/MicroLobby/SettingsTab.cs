using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ZeroKLobby.MicroLobby
{
	public partial class SettingsTab: UserControl, INavigatable
	{
		readonly ContextMenu cmDisplay;
		readonly ContextMenu cmKeybinds;

		public SettingsTab()
		{
			InitializeComponent();


			helpButton.MouseUp += helpButton_MouseUp;

			var isDesigner = Process.GetCurrentProcess().ProcessName == "devenv"; // workaround for this.DesignMode not working
			if (isDesigner) return;

			var cfRoot = Program.SpringPaths.WritableDirectory;

			cmDisplay = new ContextMenu();
			
            cmDisplay.MenuItems.Add(new MenuItem("Edit engine settings (GUI)",(o, x) => {SpringsettingForm window1 = new SpringsettingForm(); window1.ShowDialog();}));
            cmDisplay.MenuItems.Add(new MenuItem("Edit engine settings (manually)", (o, x) => Utils.SafeStart("notepad.exe", Program.SpringPaths.GetSpringConfigPath())));

			cmDisplay.MenuItems.Add(new MenuItem("Edit LUPS settings", (o, x) => Utils.SafeStart("notepad.exe", Utils.MakePath(cfRoot, "lups.cfg"))));
			cmDisplay.MenuItems.Add(new MenuItem("Edit cmdcolors", (o, x) => Utils.SafeStart("notepad.exe", Utils.MakePath(cfRoot, "cmdcolors.txt"))));
			cmDisplay.MenuItems.Add(new MenuItem("Edit ctrlpanel settings",
			                                     (o, x) => Utils.SafeStart(Utils.MakePath(cfRoot, "LuaUI", "ctrlpanel.txt"))));
            cmDisplay.MenuItems.Add(new MenuItem("Edit UI keys", (o, x) => Utils.SafeStart(Utils.MakePath(cfRoot, "uikeys.txt"))));

            Program.ToolTip.SetText(cbSafeMode,"Use safe mode - all effects reduce to minimum, use if the game is crashing");

            Program.ToolTip.SetText(cbHwCursor,"HW cursor moves faster with no lag, but it can become invisible on some machines");
            Program.ToolTip.SetText(cbMtEngine, "MT engine is experimental and it *can* improve or *decrease* performance, cause crashes and desyncs. \r\nUse at own risk");

			
		}


        bool refreshingConfig = false;
		public void RefreshConfig()
		{
            refreshingConfig = true;
			propertyGrid1.SelectedObject = Program.Conf;
            cbWindowed.Checked = Program.EngineConfigurator.GetConfigValue("WindowBorderless") == "0";
            cbHwCursor.Checked = Program.EngineConfigurator.GetConfigValue("HardwareCursor") == "1";
            tbResx.Text = Program.EngineConfigurator.GetConfigValue("XResolution");
            tbResy.Text = Program.EngineConfigurator.GetConfigValue("YResolution");
            refreshingConfig = false;
            cbSafeMode.Checked = Program.Conf.UseSafeMode;
		    cbMtEngine.Checked = Program.Conf.UseMtEngine;
		}


	    public void SaveConfig() {
            if (cbWindowed.Checked) {
                Program.EngineConfigurator.SetConfigValue("Fullscreen", "0");
                Program.EngineConfigurator.SetConfigValue("WindowBorderless", "0");
                Program.EngineConfigurator.SetConfigValue("XResolution", tbResx.Text);
                Program.EngineConfigurator.SetConfigValue("YResolution", tbResy.Text);
            } else {
                Program.EngineConfigurator.SetConfigValue("Fullscreen","0");
                Program.EngineConfigurator.SetConfigValue("WindowBorderless", "1");
                Program.EngineConfigurator.SetConfigValue("WindowState", "0");
                Program.EngineConfigurator.SetConfigValue("WindowPosY", "0");
                Program.EngineConfigurator.SetConfigValue("WindowPosX", "0");
                Program.EngineConfigurator.SetConfigValue("XResolution", "0");
                Program.EngineConfigurator.SetConfigValue("YResolution", "0");
            }
	        Program.EngineConfigurator.SetConfigValue("HardwareCursor", cbHwCursor.Checked?"1":"0");
            Program.EngineConfigurator.SetConfigValue("WindowState", "0"); // neded for borderless
            Program.Conf.UseSafeMode = cbSafeMode.Checked;
	        Program.Conf.UseMtEngine = cbMtEngine.Checked;
	    }

	    public string PathHead { get { return "settings"; } }

		public bool TryNavigate(params string[] path)
		{
			return path.Length > 0 && path[0] == PathHead;
		}

		public bool Hilite(HiliteLevel level, params string[] path)
		{
			return false;
		}

		public string GetTooltip(params string[] path)
		{
			return null;
		}

		void SettingsTab_Load(object sender, EventArgs e)
		{
			RefreshConfig();
		}

		void btnBrowse_Click(object sender, EventArgs e)
		{
			Utils.SafeStart("file://" + Program.SpringPaths.WritableDirectory);
		}

		void btnDisplay_Click(object sender, EventArgs e)
		{
			cmDisplay.Show(this, PointToClient(MousePosition));
		}

		void btnKeybindings_Click(object sender, EventArgs e)
		{
			cmKeybinds.Show(this, PointToClient(MousePosition));
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
			foreach (var admin in Program.TasClient.ExistingUsers.Values.Where(u => (u.IsAdmin || u.IsZeroKAdmin)&& !u.IsBot).OrderBy(u => u.IsAway ? 1 : 0))
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
			try
			{
                Process.Start("http://zero-k.info/Forum?categoryID=3");
			}
			catch {}
		}

		void logButton_Click(object sender, EventArgs e)
		{
			ActionHandler.ShowLog();
		}

		void problemButton_Click(object sender, EventArgs e)
		{
			try
			{
				Process.Start("http://code.google.com/p/zero-k/issues/entry");
			}
			catch {}
		}

		void siteFeatureRequestItem_Click(object sender, EventArgs e)
		{
			try
			{
				Process.Start("http://code.google.com/p/zero-k/issues/entry?template=Feature%20Request");
			}
			catch {}
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
            Program.SaveConfig();
            Application.Restart(); 
        }

        private void btnDefaults_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Do you want to reset configuration to defaults and delete all cached content?", "Local data reset", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) {
                Program.EngineConfigurator.Reset();
                var path = Program.SpringPaths.WritableDirectory;
                Directory.Delete(Utils.MakePath(path,"cache"),true);
                Directory.Delete(Utils.MakePath(path, "pool"), true);
                Directory.Delete(Utils.MakePath(path, "packages"), true);
                Directory.Delete(Utils.MakePath(path, "LuaUI"), true);
                Directory.Delete(Utils.MakePath(path, "temp"), true);
                Application.Restart();
            
            }
        }

        private void cbSafeMode_CheckedChanged(object sender, EventArgs e)
        {
            Program.Conf.UseSafeMode = cbSafeMode.Checked;
            Program.SaveConfig();
        }

        private void cbMtEngine_CheckedChanged(object sender, EventArgs e) {
            Program.Conf.UseMtEngine = cbMtEngine.Checked;
            Program.SaveConfig();
        }
	}
}