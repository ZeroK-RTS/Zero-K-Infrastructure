using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PlasmaShared;
using ZeroKLobby;

namespace ZeroKLobby.MicroLobby
{
    public partial class BattleListTab: UserControl, INavigatable
    {
    	BattleListControl battleListControl;

    	public BattleListTab()
        {
            InitializeComponent();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            if (DesignMode) return;

            // filter section
            var panel = new Panel { Dock = DockStyle.Bottom, Height = 22 };
            var filterBox = new TextBox { Dock = DockStyle.Bottom };
            filterBox.TextChanged += (s, e) => { filterBox.BackColor = String.IsNullOrEmpty(filterBox.Text) ? Color.White : Color.LightBlue; };
            panel.Controls.Add(filterBox);

            var lookingGlass = new PictureBox { Width = 20, Height = 20, Image = Resources.search, SizeMode = PictureBoxSizeMode.CenterImage, Dock = DockStyle.Left };
            panel.Controls.Add(lookingGlass);
            Program.ToolTip.SetText(lookingGlass, "Search game, description, map or player");
            Program.ToolTip.SetText(filterBox, "Search game, description, map or player");

            // battle list
            battleListControl = new BattleListControl(filterBox) { Dock = DockStyle.Fill };

            // game list
            var gameList = new BattleGameList { Dock = DockStyle.Left };
            var allGameItem = new AllGameItem { IsSelected = true };
            allGameItem.Click += (s, e) => battleListControl.GameFilter = null;
            gameList.AddItem(allGameItem);
            foreach (var gameInfo in StartPage.GameList)
            {
                var gameListItem = new BattleGameListItem(gameInfo);
                gameListItem.Click += (s, e) => battleListControl.GameFilter = gameListItem.Game;
                gameList.AddItem(gameListItem);
            }

            Controls.Add(battleListControl);
            Controls.Add(gameList);
            Controls.Add(panel);

            moreButton.MouseUp += (s, e) => battleListControl.GetContextMenu().Show(moreButton, e.Location);
        }


    	public bool TryNavigate(string pathHead, params string[] pathTail)
    	{
			if (pathHead != "battlelist") return false;
			if (pathTail.Length != 1) return true;
    		var gameShortcut = pathTail[0];
			if (!String.IsNullOrEmpty(gameShortcut))
			{
				var game = StartPage.GameList.FirstOrDefault(g => g.Shortcut == gameShortcut);
				if (game != null)
				{
					battleListControl.GameFilter = game;
				}
			}

    		return true;
    	}
    }
}