using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PlasmaDownloader;
using ZkData;
using System.Diagnostics;

namespace ZeroKLobby.MicroLobby.ExtrasTab
{
    public partial class ExtrasTab : UserControl, INavigatable
    {
        readonly ExtrasToolTabs toolTabs = new ExtrasToolTabs { Dock = DockStyle.Fill };

        public ExtrasTab()
        {
            SuspendLayout();

            if (Process.GetCurrentProcess().ProcessName == "devenv") return; // detect design mode, workaround for non-working this.DesignMode 
            Controls.Add(toolTabs);

            toolTabs.AddTab("skirmish", "Skirmish", new SkirmishControl(), ZklResources.battle, "Custom battle against AI", 3);
            //toolTabs.AddTab("mission", "Mission", new SkirmishControl(), ZklResources.battle, "not available yet", 2);

            toolTabs.SelectTab("skirmish");
            ResumeLayout();
        }

        public string PathHead { get { return "extra"; } }

        public bool TryNavigate(params string[] path) //called by NavigationControl.cs when user press Navigation button or the URL button
        {
            if (path.Length == 0) return false;
            if (path[0] != PathHead) return false;
            if (path.Length == 2 && !String.IsNullOrEmpty(path[1]))
            {
                toolTabs.SelectTab(path[1]); 
                //if (path[1] == "skirmish") toolTabs.SelectTab("skirmish");
                //else if (path[1] == "mission") toolTabs.SelectTab("mission");
                //else if (path[1] == "webmission") toolTabs.SelectTab("webmission");
                //else if (path[1] == "campaign") toolTabs.SelectTab("campaign");
            }
            return true;
            //note: the path is set from ToolTabs.cs (to NavigationControl.cs) which happens when user pressed the channel's name.
        }

        public bool Hilite(HiliteLevel level, string pathString)
        {
            var path = pathString.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (path.Length == 0) return false;
            if (path[0] != PathHead) return false;
            else if (path.Length >= 2 && !String.IsNullOrEmpty(path[1]))
            {
                return toolTabs.SetHilite(path[1], level);
            }
            return false;
        }

        public string GetTooltip(params string[] path)
        {
            return null;
        }

        public void Reload()
        {

        }

        public bool CanReload { get { return false; } }

        public bool IsBusy { get { return false; } }
    }
}
