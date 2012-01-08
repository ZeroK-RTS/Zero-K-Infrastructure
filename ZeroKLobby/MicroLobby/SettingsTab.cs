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

			feedbackButton.MouseUp += feedbackButton_MouseUp;
			helpButton.MouseUp += helpButton_MouseUp;

			var isDesigner = Process.GetCurrentProcess().ProcessName == "devenv"; // workaround for this.DesignMode not working
			if (isDesigner) return;

			var cfRoot = Program.SpringPaths.WritableDirectory;

			cmDisplay = new ContextMenu();
			cmDisplay.MenuItems.Add(new MenuItem("Edit engine settings (advanced)",
			                                     (o, x) => Utils.SafeStart("notepad.exe", Program.SpringPaths.GetSpringConfigPath())));
			cmDisplay.MenuItems.Add(new MenuItem("Edit LUPS settings (advanced)", (o, x) => Utils.SafeStart("notepad.exe", Utils.MakePath(cfRoot, "lups.cfg"))));
			cmDisplay.MenuItems.Add(new MenuItem("Edit cmdcolors (advanced)", (o, x) => Utils.SafeStart("notepad.exe", Utils.MakePath(cfRoot, "cmdcolors.txt"))));
			cmDisplay.MenuItems.Add(new MenuItem("Edit ctrlpanel settings (advanced)",
			                                     (o, x) => Utils.SafeStart(Utils.MakePath(cfRoot, "LuaUI", "ctrlpanel.txt"))));

			// keybindings
			cmKeybinds = new ContextMenu();

			cmKeybinds.MenuItems.Add(new MenuItem("Edit UI keys (advanced)", (o, x) => Utils.SafeStart(Utils.MakePath(cfRoot, "uikeys.txt"))));
			cmKeybinds.MenuItems.Add(new MenuItem("Run SelectionEditor",
			                                      (o, x) =>
			                                      	{
			                                      		var editor = Utils.MakePath(Path.GetDirectoryName(Program.SpringPaths.Executable), "SelectionEditor.exe");
			                                      		if (File.Exists(editor)) Utils.SafeStart(editor);
			                                      		else Utils.SafeStart(Utils.MakePath(cfRoot, "selectkeys.txt"));
			                                      	}));
		}


        bool refreshingConfig = false;
		public void RefreshConfig()
		{
            refreshingConfig = true;
			propertyGrid1.SelectedObject = Program.Conf;
            cbWindowed.Checked = Program.EngineConfigurator.GetConfigValue("Fullscreen") == "0";
            cbHwCursor.Checked = Program.EngineConfigurator.GetConfigValue("HardwareCursor") == "1";
            tbResx.Text = Program.EngineConfigurator.GetConfigValue("XResolution");
            tbResy.Text = Program.EngineConfigurator.GetConfigValue("YResolution");
            refreshingConfig = false;
		}


	    public void SaveConfig() {
            Program.EngineConfigurator.SetConfigValue("Fullscreen", cbWindowed.Checked?"0":"1");
            Program.EngineConfigurator.SetConfigValue("HardwareCursor", cbHwCursor.Checked?"1":"0");
            Program.EngineConfigurator.SetConfigValue("XResolution", tbResx.Text);
            Program.EngineConfigurator.SetConfigValue("YResolution", tbResy.Text);
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
		void feedbackButton_MouseUp(object sender, MouseEventArgs ea)
		{
			var menu = new ContextMenu();
			var joinItem = new MenuItem("Chat with us in the Zero-K development channel");
			joinItem.Click += (s, e) => NavigationControl.Instance.Path = "chat/channel/zkdev";
			menu.MenuItems.Add(joinItem);
			var siteItem = new MenuItem("Leave us a message on the Zero-K development site");
			siteItem.Click += siteFeatureRequestItem_Click;
			menu.MenuItems.Add(siteItem);
			menu.Show(feedbackButton, ea.Location);
		}

		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		void helpButton_MouseUp(object sender, MouseEventArgs ea)
		{
			var menu = new ContextMenu();
			var joinItem = new MenuItem("Ask in the developer channel (#zkdev)");
			joinItem.Click += (s, e) => NavigationControl.Instance.Path = "chat/channel/zkdev";
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

        private void btnWidgets_Click(object sender, EventArgs e)
        {
            Program.MainWindow.navigationControl.Path = "widgets";
        }

        private void btnRapid_Click(object sender, EventArgs e)
        {
            Program.MainWindow.navigationControl.Path = "rapid";
        }

	}
}