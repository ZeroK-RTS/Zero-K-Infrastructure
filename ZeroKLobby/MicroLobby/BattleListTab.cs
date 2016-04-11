using System;
using System.Drawing;
using System.Windows.Forms;
using ZeroKLobby.Controls;

namespace ZeroKLobby.MicroLobby
{
    public class BattleListTab: ZklBaseControl, INavigatable
    {
        private readonly BattleListControl battleListControl;
        private readonly Panel battlePanel;

        private readonly Panel panel1;
        private readonly ZklTextBox searchBox;
        private readonly Label searchLabel;

        public BattleListTab() {
            SuspendLayout();
            Size = new Size(731, 463);

            panel1 = new Panel { BackColor = Color.Transparent, Dock = DockStyle.Top, Location = new Point(0, 0), Size = new Size(731, 31) };
            searchLabel = new Label { AutoSize = true, Location = new Point(3, 7), Size = new Size(59, 18), Text = "Search:", ForeColor = Config.TextColor, Font = Config.GeneralFont};
            searchBox = new ZklTextBox { BackColor = Color.FromArgb(0, 30, 40), Location = new Point(68, 4), Size = new Size(178, 24), TabIndex = 1 };

            panel1.Controls.Add(searchLabel);
            panel1.Controls.Add(searchBox);

            battlePanel = new Panel
            {
                Location = new Point(0, 31),
                Size = new Size(731, 432),
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right
            };

            // battle list
            battleListControl = new BattleListControl { Dock = DockStyle.Fill };
            battlePanel.Controls.Add(battleListControl);

            Controls.Add(panel1);
            Controls.Add(battlePanel);

            Program.ToolTip.SetText(searchBox, "Search game, map or player");
            Program.ToolTip.SetText(searchBox.TextBox, "Search game, map or player");
            Program.ToolTip.SetText(searchLabel, "Search game, map or player");

            searchBox.TextChanged += searchBox_TextChanged;

            ResumeLayout();
        }


        public bool TryNavigate(params string[] path) {
            if (path.Length == 0) return false;
            if (path[0] != PathHead) return false;

            if (path.Length == 2 && !string.IsNullOrEmpty(path[1]))
            {
                var gameShortcut = path[1];
                if (battleListControl == null) Program.Conf.BattleFilter = gameShortcut;
                else battleListControl.FilterText = gameShortcut;
            } else
            {
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

        public void Reload() {}

        public bool CanReload { get { return false; } }

        public bool IsBusy { get { return false; } }

        private void searchBox_TextChanged(object sender, EventArgs e) {
            if (!string.IsNullOrEmpty(searchBox.Text)) Program.MainWindow.navigationControl.Path = "battles/" + searchBox.Text;
            else Program.MainWindow.navigationControl.Path = "battles";
            battleListControl.FilterText = searchBox.Text;
        }
    }
}