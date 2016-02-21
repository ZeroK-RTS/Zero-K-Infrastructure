using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ZeroKLobby.MainPages;
using ZkData;

namespace ZeroKLobby.MicroLobby
{
    public partial class BattleListTab: UserControl, INavigatable, IMainPage
    {
        BattleListControl battleListControl;

        public BattleListTab() 
        {
            InitializeComponent();
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

            SuspendLayout(); //pause

            if (this.IsInDesignMode()) return;
            var lookingGlass = new PictureBox
            {
                Width = (int)20,
                Height = (int)20,
                Image = ZklResources.search,
                SizeMode = PictureBoxSizeMode.CenterImage,
                Dock = DockStyle.Left
            };
            Program.ToolTip.SetText(lookingGlass, "Search game, description, map or player");
            Program.ToolTip.SetText(searchBox, "Search game, description, map or player");

            hideEmptyBox.Checked = Program.Conf.HideEmptyBattles;
            hideFullBox.Checked = Program.Conf.HideNonJoinableBattles;
            showOfficialBox.Checked = Program.Conf.ShowOfficialBattles;
            hidePasswordedBox.Checked = Program.Conf.HidePasswordedBattles;

            // battle list
            battleListControl = new BattleListControl() { Dock = DockStyle.Fill, VerticalScroll = { Visible = false}};


            this.battleListControl.SizeChanged += BattleListControl_SizeChanged;

            this.battleListControl.MouseWheel += BattleListControl_MouseWheel;
            this.customScrollbar.Scroll += CustomScrollbar_Scroll;

            battlePanel.Controls.Add(battleListControl);
            ResumeLayout();
        }

        private void BattleListControl_SizeChanged(object sender, EventArgs e)
        {
            this.customScrollbar.Maximum = this.battleListControl.VerticalScroll.Maximum;
            this.customScrollbar.Minimum = this.battleListControl.VerticalScroll.Minimum;
            this.customScrollbar.LargeChange = this.battleListControl.VerticalScroll.LargeChange;
            this.customScrollbar.SmallChange = this.battleListControl.VerticalScroll.SmallChange;
        }

        private void CustomScrollbar_Scroll(object sender, EventArgs e)
        {
            battleListControl.AutoScrollPosition = new Point(0, this.customScrollbar.Value);
            this.customScrollbar.Invalidate();
            Application.DoEvents();
        }

        private void BattleListControl_MouseWheel(object sender, MouseEventArgs e)
        {
            customScrollbar.Value = this.battleListControl.VerticalScroll.Value;
            this.customScrollbar.Invalidate();
            Application.DoEvents();
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



        void searchBox_TextChanged(object sender, EventArgs e) {
            if (!string.IsNullOrEmpty(searchBox.Text)) Program.MainWindow.navigationControl.Path = "battles/" + searchBox.Text;
            else Program.MainWindow.navigationControl.Path = "battles";
            battleListControl.FilterText = searchBox.Text;
        }

        void showEmptyBox_CheckedChanged(object sender, EventArgs e) {
            if (battleListControl != null) battleListControl.HideEmpty = hideEmptyBox.Checked;
        }

        void showFullBox_CheckedChanged(object sender, EventArgs e) {
            if (battleListControl != null) battleListControl.HideFull = hideFullBox.Checked;
        }

        void showOfficialButton_CheckedChanged(object sender, EventArgs e) {
            if (battleListControl != null) battleListControl.ShowOfficial = showOfficialBox.Checked;
        }

        private void hidePasswordedBox_CheckedChanged(object sender, EventArgs e)
        {
            if (battleListControl != null) battleListControl.HidePassworded = hidePasswordedBox.Checked;
        }

        public void GoBack()
        {
            Program.MainWindow.SwitchPage(MainWindow.MainPages.MultiPlayer, false);
        }

        public Image MainWindowBgImage { get { return BgImages.blue_galaxy; }}
    }
}