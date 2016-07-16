using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using LobbyClient;
using ZkData;
using ZeroKLobby.MicroLobby;

namespace ZeroKLobby
{
    class PlayerTooltipRenderer:IToolTipRenderer
    {
        string userName;

        public void SetPlayerTooltipRenderer(string name)
        {
            userName = name;
        }

        public void Draw(Graphics g, Font font, Color foreColor)
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            var fbrush = new SolidBrush(foreColor);
            User user;
            if (!Program.TasClient.ExistingUsers.TryGetValue(userName, out user)) return;
            var x = (int)1;
            var y = 3;
            Action newLine = () =>
                {
                x = (int)1;
                y += (int)16;
                };
            Action<string> drawString = (text) =>
            {
                //y -= 3;
                x += ToolTipHandler.TEXT_X_OFFSET;
                TextRenderer.DrawText(g, text, font, new Point(x, y + ToolTipHandler.TEXT_Y_OFFSET), Config.TextColor, TextFormatFlags.LeftAndRightPadding);
                x += TextRenderer.MeasureText(g, text, font).Width;
                //y += 3;
            };
            
            Action<string, Color> drawString2 = (text, color) =>
            {
                //y -= 3;
                x += ToolTipHandler.TEXT_X_OFFSET;
                TextRenderer.DrawText(g, text, font, new Point(x, y + ToolTipHandler.TEXT_Y_OFFSET), color, TextFormatFlags.LeftAndRightPadding);
                x += TextRenderer.MeasureText(g, text, font).Width;
                //y += 3;
            };


            Action<Image, int, int> drawImage = (image, w, h) =>
                {
                    g.DrawImage(image, x, y, (int)w, (int)h);
                    x += (int)(w + 3);
                };
            using (var boldFont = new Font(font, FontStyle.Bold)) TextRenderer.DrawText(g, user.Name, boldFont, new Point(x, y), Config.TextColor, TextFormatFlags.LeftAndRightPadding);
            y += 3;
            newLine();

            if (!user.IsBot)
            {
                var clan = ServerImagesHandler.GetClanOrFactionImage(user);
                if (clan.Item1 != null)
                {
                    drawImage(clan.Item1, 16, 16);
                    drawString2(clan.Item2, Utils.GetFactionColor(user.Faction));
                    newLine();
                }
            }

            Image flag;
            if (Images.CountryFlags.TryGetValue(user.Country, out flag) && flag != null)
            {
                //drawString("Country: ");
                y += 2;
                drawImage(flag, flag.Width, flag.Height);
                y -= 2;
                drawString(CountryNames.GetName(user.Country));
                newLine();
            }
            if (user.IsBot)
            {
                drawImage(ZklResources.robot, 16, 16);
                drawString("Bot");
                newLine();
            }
            if (user.IsAdmin)
            {
                drawImage(ZklResources.police, 16, 16);
                drawString("Administrator");
                newLine();
            }
            if (Program.FriendManager.Friends.Contains(user.Name))
            {
                drawImage(ZklResources.friend, 16, 16);
                drawString("Friend");
                newLine();
            }
            if (user.SteamID != null)
            {
                drawImage(ZklResources.steam, 16, 16);
                drawString(string.Format("Steam name: {0}", user.DisplayName ?? user.Name));
                newLine();
            }
            if (!user.IsBot)
            {
                drawImage(Images.GetRank(user.Level), 16, 16);
                drawString(string.Format("Level: {0}", user.Level));
                newLine();
                if (user.AwaySince.HasValue)
                {
                    drawImage(ZklResources.away, 16, 16);
                    drawString("User idle for " + DateTime.UtcNow.Subtract(user.AwaySince.Value).PrintTimeRemaining() + ".");
                    newLine();
                }
                if (user.IsInGame)
                {
                    drawImage(ZklResources.ingame, 16, 16);
                    if (user.InGameSince != null) {
                        var time = DateTime.UtcNow.Subtract(user.InGameSince.Value).PrintTimeRemaining();
                        drawString("Playing since " + time + " ago.");
                    }
                    newLine();
                }
                var top10 = Program.SpringieServer.GetTop10Rank(user.Name);
                if (top10 > 0)
                {
                    drawImage(ZklResources.cup, 16, 16);
                    drawString(string.Format("Top 10 Rank: {0}.", top10));
                    newLine();
                }
                if (!string.IsNullOrEmpty(user.Avatar))
                {
                    var image = Program.ServerImages.GetAvatarImage(user);
                    if (image != null) g.DrawImage(image, (int)(302 - 65), 0, (int)64, (int)64);
                }

            }
            if (user.IsInBattleRoom)
            {
                if (y < 70) y = 70; 
                var battle = Program.TasClient.ExistingBattles.Values.FirstOrDefault(b => b.Users.ContainsKey(user.Name));
                if (battle != null) {
                    var battleIcon = Program.BattleIconManager.GetBattleIcon(battle.BattleID);
                    if (battleIcon != null) g.DrawImageUnscaled(battleIcon.Image, x, y);
                }
            }
            fbrush.Dispose();

        }


        public Size? GetSize(Font font)
        {
            User user;
            if (!Program.TasClient.ExistingUsers.TryGetValue(userName, out user)) return null;
            var h = 0;
            h += 16+3; // name
            h += 16; // flag
            if (user.IsBot) h += 16; // bot icon
            if (user.IsAdmin) h += 16; // admin icon
            if (Program.FriendManager.Friends.Contains(user.Name)) h += 16; // friend icon
            if (!user.IsBot)
            {
                h += 16; // rank text
                if (user.IsAway) h += 16; // away icon
                if (user.IsInGame) h += 16; // ingame icon
                h += 16; // skill text
            }
            if (user.SteamID!=null) h += 16; //steam icon
            if (Program.SpringieServer.GetTop10Rank(user.Name) > 0) h += 16; // top 10
            if (user.IsInBattleRoom) h += 76; // battle icon

            return new Size((int)302, (int)h);
        }
    }
}