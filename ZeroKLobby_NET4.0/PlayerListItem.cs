using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;
using LobbyClient;
using SpringDownloader.MicroLobby;

namespace SpringDownloader
{
    public class PlayerListItem
    {
        public bool IsGrayedOut { get; set; }
        public int SortCategory { get; set; }
        public string Title { get; set; }
        public int? AllyTeam { get; set; }
        public bool IsSpectatorsTitle { get; set;}
        public BotBattleStatus BotBattleStatus { get; set; }
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
        public UserBattleStatus UserBattleStatus
        {
            get
            {
                return Program.TasClient.MyBattle == null ? null : Program.TasClient.MyBattle.Users.SingleOrDefault(u => u.Name == UserName);
            }
        }

        public string UserName { get; set; }

        public void DrawPlayerLine(Graphics g, Font font, Font boldFont, Rectangle bounds, Color foreColor, Color backColor, bool grayedOut, bool isBattle)
        {
            var x = 0;

            if (grayedOut) foreColor = Color.Gray;

            Action<Image> drawImageUnscaled = image =>
                {
                    g.DrawImageUnscaled(image, bounds.Left + x, bounds.Top);
                    x += image.Width;
                    
                };

            Action<Image, int, int> drawImage = (image, w, h) =>
                {
                    g.DrawImage(image, bounds.Left + x, bounds.Top, w, h);
                    x += w;
                };

            Action<string, Color, Color> drawText = (text, fore, back) =>
                {
                    TextRenderer.DrawText(g, text, font, new Point(bounds.Left + x, bounds.Top), fore, back);
                    x += TextRenderer.MeasureText(g, text, font).Width;
                };

            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // is section header
            if (Title != null) {
                font = boldFont;
                if (Title == "Search results:") drawImage(Images.Search, 11, 11);
                drawText(Title, foreColor, backColor);
                return;
            }

            // is bot
            if (BotBattleStatus != null)
            {
                var bot = BotBattleStatus;
                drawImage(Images.Robot, 11, 11);
                var text = String.Format("{0} ({1} added by {2}", bot.Name, bot.aiLib, bot.owner);
                drawText(text, foreColor, backColor);
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
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                if (userStatus.IsSpectator) drawImage(Images.Spec, 11, 11);
                else if (userStatus.IsReady) drawImage(Images.ReadyImage, 11, 11);
                else drawImage(Images.UnreadyImage, 11, 11);
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
            }

            if (user.IsBot) drawImage(Images.Robot, 11, 11);
            else if (Program.FriendManager.Friends.Contains(user.Name)) drawImage(Images.Friend, 11, 11);
            else if (user.IsAdmin) drawImage(Images.Police, 11, 11);
            else drawImage(Images.User, 11, 11);

            Image flag;
            if (Images.CountryFlags.TryGetValue(user.Country, out flag) && flag != null) drawImageUnscaled(flag);
            else x += 16;
            drawImage(Images.RankImages[Math.Min(user.Rank, 6)], 11, 11); // TODO: support new rank(s)

            drawText(user.Name, foreColor, backColor);
            var top10 = Program.SpringieServer.GetTop10Rank(user.Name);
            if (top10 > 0)
            {
                var oldProgression = x;
                drawImage(Images.Cup, 11, 11);
                x = oldProgression;
                drawText(top10.ToString(), Color.FromArgb(50, 150, 50, 50), Color.Transparent);
            }
            if (user.IsInGame)
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                drawImage(Images.Ingame, 11, 11);
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
            }
            if (user.IsInBattleRoom && !user.IsInGame) drawImage(Images.SmallBattle, 11, 11);
            var qmInfo = Program.QuickMatchTracker.GetQuickMatchInfo(user.Name);
            if (qmInfo != null)
            {
                drawImage(Images.SpringDownloaderLogo, 11, 11);
                drawText(qmInfo.ToString(), foreColor, backColor);
            }
        }

        public override string ToString()
        {
            var name = string.Empty;
            if (UserName != null) name = UserName;
            else if (UserBattleStatus != null) name = UserBattleStatus.Name;
            else if (Title != null) name = Title;
            return SortCategory.ToString("00000") + name;
        }
    }
}