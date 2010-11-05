using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using LobbyClient;
using PlasmaShared.UnitSyncLib;

namespace ZeroKLobby.MicroLobby
{
	public class PlayerListItem: IDisposable
	{
		readonly Font boldFont = new Font("Microsoft Sans Serif", 9, FontStyle.Bold);
		Font font = new Font("Segoe UI", 9, FontStyle.Regular);
		int height = 16;
		public int? AllyTeam { get; set; }
		public BotBattleStatus BotBattleStatus { get; set; }
		public string Button { get; set; }
		public int Height { get { return height; } set { height = value; } }
		public bool IsGrayedOut { get; set; }
		public bool IsSpectatorsTitle { get; set; }
		public int SortCategory { get; set; }
		public string Title { get; set; }
		public MissionSlot MissionSlot { get; set; }
		public User User
		{
			get
			{
				if (UserName == null) return null;
				User user;
				Program.TasClient.ExistingUsers.TryGetValue(UserName, out user);
				return user;
			}
		}
		public UserBattleStatus UserBattleStatus { get { return Program.TasClient.MyBattle == null ? null : Program.TasClient.MyBattle.Users.SingleOrDefault(u => u.Name == UserName); } }

		public string UserName { get; set; }


		~PlayerListItem()
		{
			Dispose();
			GC.SuppressFinalize(this);
		}

		public void Dispose()
		{
			font.Dispose();
			boldFont.Dispose();
		}

		public void DrawPlayerLine(Graphics g, Rectangle bounds, Color foreColor, Color backColor, bool grayedOut, bool isBattle)
		{
			g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
			g.InterpolationMode = InterpolationMode.HighQualityBicubic;

			var x = 0;

			if (grayedOut) foreColor = Program.Conf.FadeColor;

			Action<Image> drawImage = image =>
				{
					g.DrawImage(image, bounds.Left + x, bounds.Top, 16, 16);
					x += 19;
				};

			Action<string, Color, Color> drawText = (text, fore, back) =>
				{
					TextRenderer.DrawText(g, text, font, new Point(bounds.Left + x, bounds.Top), fore, back);
					x += TextRenderer.MeasureText(g, text, font).Width;
				};

			// is section header
			if (Title != null)
			{
				font = boldFont;
				if (Title == "Search results:") drawImage(Resources.search);
				TextRenderer.DrawText(g, Title, font, bounds, foreColor, backColor, TextFormatFlags.HorizontalCenter);
				return;
			}

			// is button
			if (Button != null)
			{
				font = boldFont;
				ButtonRenderer.DrawButton(g, bounds, Button, font, false, PushButtonState.Normal);
				return;
			}

			// is bot
			if (BotBattleStatus != null)
			{
				var bot = BotBattleStatus;
				x += 19;
				var botColor = BotBattleStatus.TeamColorRGB;
				using (var brush = new SolidBrush(Color.FromArgb(botColor[0], botColor[1], botColor[2])))
				{
					g.SmoothingMode = SmoothingMode.AntiAlias;
					g.FillEllipse(brush, x, bounds.Top, bounds.Bottom - bounds.Top, bounds.Bottom - bounds.Top);
				}
				drawImage(Resources.robot);
				var botDisplayName = MissionSlot == null ? bot.aiLib : MissionSlot.TeamName;
				drawText(botDisplayName, foreColor, backColor);
				return;
			}

			// is player
			var user = User;
			var userStatus = UserBattleStatus;

			if (UserName != null && user == null)
			{
				drawText(UserName + " has left.", foreColor, backColor);
				return;
			}

			if (isBattle)
			{
				if (userStatus.IsSpectator) drawImage(Resources.spec);
				else if (userStatus.IsReady && userStatus.SyncStatus == SyncStatuses.Synced) drawImage(Resources.ready);
				else drawImage(Resources.unready);

				if (!userStatus.IsSpectator)
				{
					var userColor = userStatus.TeamColorRGB;
					using (var brush = new SolidBrush(Color.FromArgb(userColor[0], userColor[1], userColor[2])))
					{
						g.SmoothingMode = SmoothingMode.AntiAlias;
						g.FillEllipse(brush, x, bounds.Top, bounds.Bottom - bounds.Top, bounds.Bottom - bounds.Top);
					}
				}
			}

			drawImage(TextImage.GetUserImage(user.Name));

			Image flag;
			if (Images.CountryFlags.TryGetValue(user.Country, out flag) && flag != null) g.DrawImageUnscaled(flag, bounds.Left + x, bounds.Top + 4);
			x += 16;
			x += 2; // margin
			drawImage(Images.GetRank(user.Rank));

			var userDisplayName = user.Name;
			if (MissionSlot != null) userDisplayName += String.Format(" ({0})", MissionSlot.TeamName);
			drawText(userDisplayName, foreColor, backColor);
			var top10 = Program.SpringieServer.GetTop10Rank(user.Name);
			if (top10 > 0)
			{
				var oldProgression = x;
				drawImage(Resources.Cup);
				x = oldProgression;
				TextRenderer.DrawText(g, top10.ToString(), boldFont, new Point(bounds.Left + x + 1, bounds.Top), Color.Black, Color.Transparent);
				x += 16;
			}

			if (user.IsInGame) drawImage(Resources.ingame);
			else if (!isBattle && user.IsInBattleRoom) drawImage(Resources.Battle);

			if (isBattle && !userStatus.IsSpectator)
			{
				var players = Program.TasClient.MyBattle.Users.Where(u => !u.IsSpectator && u.Name != userStatus.Name);
				var bots = Program.TasClient.MyBattle.Bots;
				var playerSharers = players.Where(p => p.TeamNumber == userStatus.TeamNumber).Select(p => p.Name);
				var botSharers = bots.Where(b => b.TeamNumber == userStatus.TeamNumber).Select(b => b.Name);
				var commSharers = playerSharers.Concat(botSharers).ToArray();
				if (commSharers.Any()) drawText("Sharing with " + String.Join(", ", commSharers), Color.Red, backColor);
			}
			var qmInfo = Program.QuickMatchTracker.GetQuickMatchInfo(user.Name);
			if (qmInfo != null)
			{
				g.InterpolationMode = InterpolationMode.NearestNeighbor;
				g.DrawImage(Resources.ZK_logo_square, bounds.Left + x + 3, bounds.Top + 4, 11, 11);
				x += 12;
				drawText(qmInfo.ToString(), foreColor, backColor);
			}
		}

		public override string ToString()
		{
			var name = string.Empty;
			if (UserBattleStatus != null)
			{
				name = UserBattleStatus.Name;
				if (UserBattleStatus.IsSpectator) SortCategory = 101;
				else SortCategory = UserBattleStatus.AllyNumber*2 + 1;
			}
			else if (BotBattleStatus != null)
			{
				name = BotBattleStatus.Name;
				SortCategory = BotBattleStatus.AllyNumber*2 + 1;
			}
			else if (UserName != null) name = UserName;
			else if (Title != null) name = Title;
			return SortCategory.ToString("00000") + name;
		}
	}
}