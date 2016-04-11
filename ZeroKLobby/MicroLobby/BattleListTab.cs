using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Forms;
using ZeroKLobby.Controls;
using ZkData;

namespace ZeroKLobby.MicroLobby
{
    public partial class BattleListTab: ZklBaseControl, INavigatable
    {
        BattleListControl battleListControl;

        public BattleListTab() 
        {
            InitializeComponent();

            SuspendLayout(); 

            Program.ToolTip.SetText(searchBox, "Search game, map or player");
            Program.ToolTip.SetText(searchLabel, "Search game, map or player");

            // battle list
            battleListControl = new BattleListControl() { Dock = DockStyle.Fill };
            battlePanel.Controls.Add(battleListControl);
            ResumeLayout();
        }

        void BattleListTab_Enter(object sender, EventArgs e) //lazy initialization
        {
            Paint -= BattleListTab_Enter; //using "Paint" instead of "Enter" event because "Enter" is too lazy in Mono (have to click control)
            
            InitializeComponent();

            if (DesignMode) return;
            
        }

        public bool TryNavigate(params string[] path) {
            if (path.Length == 0) return false;
            if (path[0] != PathHead) return false;

            if (path.Length == 2 && !String.IsNullOrEmpty(path[1])) {
                var gameShortcut = path[1];
                if (battleListControl == null) Program.Conf.BattleFilter = gameShortcut;
                else battleListControl.FilterText = gameShortcut;
            }
            else {
                if (battleListControl == null) Program.Conf.BattleFilter = "";
                else battleListControl.FilterText = "";
            }
            return true;
        }


        public string PathHead { get { return "battles"; } }

        public bool Hilite(HiliteLevel level, string path) {
            return false;
        }

        public string GetTooltip(params string[] path) {
            return null;
        }

        public void Reload() {
        }

        public bool CanReload { get { return false; } }

        public bool IsBusy { get { return false;} }

        void searchBox_TextChanged(object sender, EventArgs e) {
            if (!string.IsNullOrEmpty(searchBox.Text)) Program.MainWindow.navigationControl.Path = "battles/" + searchBox.Text;
            else Program.MainWindow.navigationControl.Path = "battles";
            battleListControl.FilterText = searchBox.Text;
        }

       
    }
}