using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using PlasmaShared;

namespace ZeroKLobby.MicroLobby
{
	public partial class SettingsTab: UserControl, INavigatable
	{
		readonly ContextMenu cmDisplay;
		readonly ContextMenu cmKeybinds;

		public SettingsTab()
		{
			InitializeComponent();

			var isDesigner = Process.GetCurrentProcess().ProcessName == "devenv"; // workaround for this.DesignMode not working
			if (isDesigner) return;

			var cfRoot = Path.GetDirectoryName(Program.SpringPaths.Executable);

			cmDisplay = new ContextMenu();
			cmDisplay.MenuItems.Add(new MenuItem("Run SpringSettings",
			                                     (o, x) =>
			                                     Utils.SafeStart(Utils.MakePath(Path.GetDirectoryName(Program.SpringPaths.Executable), "springsettings.exe"))));

			cmDisplay.MenuItems.Add(new MenuItem("Edit engine settings (advanced)", (o, x) => Utils.SafeStart(SpringPaths.GetSpringConfigPath())));
			cmDisplay.MenuItems.Add(new MenuItem("Edit LUPS settings (advanced)", (o, x) => Utils.SafeStart(Utils.MakePath(cfRoot, "lups.cfg"))));
			cmDisplay.MenuItems.Add(new MenuItem("Edit ctrlpanel settings (advanced)",
			                                     (o, x) => Utils.SafeStart(Utils.MakePath(cfRoot, "LuaUI", "ctrlpanel.txt"))));

			// keybindings
			cmKeybinds = new ContextMenu();
			cmKeybinds.MenuItems.Add(new MenuItem("Run SelectionEditor",
			                                      (o, x) =>
			                                      Utils.SafeStart(Utils.MakePath(Path.GetDirectoryName(Program.SpringPaths.Executable), "SelectionEditor.exe"))));
			cmKeybinds.MenuItems.Add(new MenuItem("Edit UI keys (advanced)", (o, x) => Utils.SafeStart(Utils.MakePath(cfRoot, "uikeys.txt"))));

			cmKeybinds.MenuItems.Add(new MenuItem("Edit select keys (advanced)", (o, x) => Utils.SafeStart(Utils.MakePath(cfRoot, "selectkeys.txt"))));
		}


		public void RefreshConfig()
		{
			propertyGrid1.SelectedObject = Program.Conf;
		}

		void SettingsTab_Load(object sender, EventArgs e)
		{
			RefreshConfig();
		}


		void btnDisplay_Click(object sender, EventArgs e)
		{
			cmDisplay.Show(this, PointToClient(MousePosition));
		}

		void btnKeybindings_Click(object sender, EventArgs e)
		{
			cmKeybinds.Show(this, PointToClient(MousePosition));
		}

		public string PathHead { get { return "settings"; } }

		public bool TryNavigate(params string[] path)
		{
			return path.Length > 0 && path[0] == PathHead;
		}

		public void Hilite(HiliteLevel level, params string[] path)
		{
			throw new NotImplementedException();
		}

		public string GetTooltip(params string[] path)
		{
			throw new NotImplementedException();
		}
	}
}