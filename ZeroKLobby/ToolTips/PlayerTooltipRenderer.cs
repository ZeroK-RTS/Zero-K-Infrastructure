using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using LobbyClient;
using PlasmaShared;
using ZeroKLobby.MicroLobby;

namespace ZeroKLobby
{
    class PlayerTooltipRenderer:IToolTipRenderer
    {
        double loadedElo;
        string loadedEloPlayerName;
        double loadedW;
        string userName;

        public PlayerTooltipRenderer(string name)
        {
            userName = name;
        }

        public void Draw(Graphics g, Font font, Color foreColor)
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            User user;
            if (!Program.TasClient.ExistingUsers.TryGetValue(userName, out user)) return;
            var x = 1;
            var y = 0;
            Action newLine = () =>
                {
                    x = 1;
                    y += 16;
                };
            Action<string> drawString = text =>
                {
                    TextRenderer.DrawText(g, text, font, new Point(x, y), foreColor);
                    x += (int)Math.Ceiling((double)TextRenderer.MeasureText(g, text, font).Width);
                };
            Action<Image, int, int> drawImage = (image, w, h) =>
                {
                    g.DrawImage(image, x, y, w, h);
                    x += w + 3;
                };
            using (var boldFont = new Font(font, FontStyle.Bold)) TextRenderer.DrawText(g, user.Name, boldFont, new Point(x, y), foreColor);

            newLine();
            drawString("Country: ");
            Image flag;
            if (Images.CountryFlags.TryGetValue(user.Country, out flag) && flag != null)
            {
                drawImage(flag, flag.Width, flag.Height);
                drawString(user.CountryName);
                newLine();
            }
            if (user.IsBot)
            {
							drawImage(Resources.robot, 16, 16);
                drawString("Bot");
                newLine();
            }
            if (user.IsAdmin)
            {
							drawImage(Resources.police, 16, 16);
                drawString("Administrator");
                newLine();
            }
            if (Program.FriendManager.Friends.Contains(user.Name))
            {
							drawImage(Resources.Friend, 16, 16);
                drawString("Friend");
                newLine();
            }
            var quickMatchInfo = Program.QuickMatchTracker.GetQuickMatchInfo(user.Name);
            if (quickMatchInfo != null)
            {
							drawImage(Resources.ZK_logo_square, 16, 16);
                drawString("ZK User " + quickMatchInfo);
                newLine();
            }
            if (!user.IsBot)
            {
                drawImage(Images.GetRank(user.Rank), 16, 16);
                var rankText = user.Rank == 0 ? "Beginner" : string.Format(" > {0} hours", User.RankLimits[user.Rank]);
                drawString("Experience: " + rankText);
                newLine();
                if (user.IsAway)
                {
                    drawImage(Resources.AwayImage, 16, 16);
                    drawString("User has been idle for " + DateTime.Now.Subtract(user.AwaySince.Value).PrintTimeRemaining() + ".");
                    newLine();
                }
                if (user.IsInGame)
                {
									drawImage(Resources.ingame, 16, 16);
                    var time = DateTime.Now.Subtract(user.InGameSince.Value).PrintTimeRemaining();
                    drawString("Playing since " + time + " ago.");
                    newLine();
                }
                var top10 = Program.SpringieServer.GetTop10Rank(user.Name);
                if (top10 > 0)
                {
									drawImage(Resources.Cup, 16, 16);
                    drawString(string.Format("Top 10 Rank: {0}.", top10));
                    newLine();
                }

                //double elo; // synced elo loading
                //double w;
                //Program.SpringieServer.GetElo(hoverUser.Name, out elo, out w);
                //if (w > 0.2) drawString("Skill: " + Math.Round(elo));
                //else drawString("Skill: unknown");

                if (loadedEloPlayerName == user.Name)
                {
                    if (loadedW > 2) drawString("Skill: " + Math.Round(loadedElo));
                    else drawString("Skill: unknown");
                }
                else
                {
                    drawString("Skill: loading...");
                    if (loadedEloPlayerName != user.Name)
                    {
                        Program.SpringieServer.GetEloAsync(user.Name,
                                                           (player, elo, w, token) =>
                                                               {
                                                                   if (userName == user.Name)
                                                                   {
                                                                       loadedEloPlayerName = userName;
                                                                       loadedElo = elo;
                                                                       loadedW = w;
                                                                   }
                                                               },
                                                           null);
                    }
                }
                newLine();
            }
            if (user.IsInBattleRoom)
            {
                var battle = Program.TasClient.ExistingBattles.Values.SingleOrDefault(b => b.Users.Any(ub => ub.Name == user.Name));
                var battleIcon = Program.BattleIconManager.GetBattleIcon(battle.BattleID);
                if (battleIcon != null) g.DrawImageUnscaled(battleIcon.Image, x, y);
            }
        }


        public Size? GetSize(Font font)
        {
            User user;
            if (!Program.TasClient.ExistingUsers.TryGetValue(userName, out user)) return null;
            var h = 0;
            h += 16; // name
            h += 16; // flag
            if (user.IsBot) h += 16; // bot icon
            if (user.IsAdmin) h += 16; // admin icon
            if (Program.FriendManager.Friends.Contains(user.Name)) h += 16; // friend icon
            if (Program.QuickMatchTracker.GetQuickMatchInfo(user.Name) != null) h += 16; // SD icon
            if (!user.IsBot)
            {
                h += 16; // rank text
                if (user.IsAway) h += 16; // away icon
                if (user.IsInGame) h += 16; // ingame icon
                h += 16; // skill text
            }
            if (Program.SpringieServer.GetTop10Rank(user.Name) > 0) h += 16; // top 10
            if (user.IsInBattleRoom) h += 76; // battle icon

            return new Size(302, h);
        }
    }
}