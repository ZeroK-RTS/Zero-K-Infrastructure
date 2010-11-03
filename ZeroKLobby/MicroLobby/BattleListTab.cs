using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ZeroKLobby.MicroLobby
{
	public partial class BattleListTab: UserControl, INavigatable
	{
		AllGameItem allGameItem;
		BattleListControl battleListControl;
		BattleGameList gameList;

		public BattleListTab()
		{
			InitializeComponent();
		}

		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		protected override void OnCreateControl()
		{
			base.OnCreateControl();
			if (DesignMode) return;

			// filter section
			var panel = new Panel { Dock = DockStyle.Bottom, Height = 22 };
			var filterBox = new TextBox { Dock = DockStyle.Bottom };
			filterBox.TextChanged += (s, e) => { filterBox.BackColor = String.IsNullOrEmpty(filterBox.Text) ? Color.White : Color.LightBlue; };
			panel.Controls.Add(filterBox);

			var lookingGlass = new PictureBox
			                   { Width = 20, Height = 20, Image = Resources.search, SizeMode = PictureBoxSizeMode.CenterImage, Dock = DockStyle.Left };
			panel.Controls.Add(lookingGlass);
			Program.ToolTip.SetText(lookingGlass, "Search game, description, map or player");
			Program.ToolTip.SetText(filterBox, "Search game, description, map or player");

			// battle list
			battleListControl = new BattleListControl(filterBox) { Dock = DockStyle.Fill };

			// game list
			gameList = new BattleGameList { Dock = DockStyle.Left };
			allGameItem = new AllGameItem { IsSelected = true };
			allGameItem.Click += (s, e) => NavigationControl.Instance.Path = "battles/all";
			gameList.AddItem(allGameItem);
			foreach (var gameInfo in StartPage.GameList)
			{
				var gameListItem = new BattleGameListItem(gameInfo);
				gameListItem.Click += (s, e) =>
					{
						NavigationControl.Instance.Path = "battles/" + gameListItem.Game.Shortcut;
						// battleListControl.GameFilter = gameListItem.Game;
					};
				gameList.AddItem(gameListItem);
			}

			Controls.Add(battleListControl);
			Controls.Add(gameList);
			Controls.Add(panel);

			moreButton.MouseUp += (s, e) => battleListControl.GetContextMenu().Show(moreButton, e.Location);
		}

		public string PathHead { get { return "battles"; } }

		public bool TryNavigate(params string[] path)
		{
			if (path.Length == 0) return false;
			if (path[0] != PathHead) return false;

			if (path.Length == 2 && !String.IsNullOrEmpty(path[1]))
			{
				var gameShortcut = path[1];
				var game = StartPage.GameList.FirstOrDefault(g => g.Shortcut == gameShortcut);
				if (game != null)
				{
					var gameListItem = gameList.Items.Cast<BattleGameListItem>().Single(i => i.Game == game);
					gameList.SelectItem(gameListItem);
					battleListControl.GameFilter = game;
					return true;
				}
			}

			if (gameList != null)
			{
				gameList.SelectItem(allGameItem);
				battleListControl.GameFilter = null;
			}

			return true;
		}

		public bool Hilite(HiliteLevel level, params string[] path)
		{
			return false;
		}

		public string GetTooltip(params string[] path)
		{
			return null;
		}
	}
}