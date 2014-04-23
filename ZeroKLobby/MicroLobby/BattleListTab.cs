using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Forms;

namespace ZeroKLobby.MicroLobby
{
    public partial class BattleListTab: UserControl, INavigatable
    {
        BattleListControl battleListControl;

        public BattleListTab() {
            SuspendLayout(); //pause
            InitializeComponent();

            if (DesignMode) return;
            DpiMeasurement.DpiXYMeasurement(this);
            var lookingGlass = new PictureBox
            {
                Width =  DpiMeasurement.ScaleValueY(20),
                Height = DpiMeasurement.ScaleValueY(20),
                Image = ZklResources.search,
                SizeMode = PictureBoxSizeMode.CenterImage,
                Dock = DockStyle.Left
            };
            Program.ToolTip.SetText(lookingGlass, "Search game, description, map or player");
            Program.ToolTip.SetText(searchBox, "Search game, description, map or player");

            showEmptyBox.Checked = Program.Conf.ShowEmptyBattles;
            showFullBox.Checked = Program.Conf.ShowNonJoinableBattles;
            showOfficialBox.Checked = Program.Conf.ShowOfficialBattles;

            // battle list
            battleListControl = new BattleListControl() { Dock = DockStyle.Fill };
            battlePanel.Controls.Add(battleListControl);
            ResumeLayout();
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

        void showEmptyBox_CheckedChanged(object sender, EventArgs e) {
            if (battleListControl != null) battleListControl.ShowEmpty = showEmptyBox.Checked;
        }

        void showFullBox_CheckedChanged(object sender, EventArgs e) {
            if (battleListControl != null) battleListControl.ShowFull = showFullBox.Checked;
        }

        void showOfficialButton_CheckedChanged(object sender, EventArgs e) {
            if (battleListControl != null) battleListControl.ShowOfficial = showOfficialBox.Checked;
        }

    }
}