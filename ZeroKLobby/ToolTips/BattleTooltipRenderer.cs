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
    public class BattleTooltipRenderer:IToolTipRenderer
    {
        int battleID;

        public BattleTooltipRenderer(int battleID)
        {
            this.battleID = battleID;
        }


        private bool GetBattleAndFounder(out Battle battle, out User founder)
        {
            founder = null;
            if (Program.TasClient.ExistingBattles.TryGetValue(battleID, out battle)) {
                founder = battle.Founder;
                return true;
            }
            return false;
        }

        public void Draw(Graphics g,  Font font, Color foreColor)
        {
            Battle battle;
            User founder;
            if (!GetBattleAndFounder(out battle, out founder)) return;

            var x = 1; // margin
            var y = 3;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
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
            founder = battle.Founder;
            drawString("Founder: " + battle.Founder);
            newLine();
            drawString("Map: " + battle.MapName);
            newLine();
            drawString("Players: " + battle.NonSpectatorCount);
            drawString("Spectators: " + battle.SpectatorCount);
            drawString("Friends: " + battle.Users.Count(u => Program.FriendManager.Friends.Contains(u.Name)));
            newLine();

            if (battle.Rank > 0)
            {
                drawImage(Images.GetRank(battle.Rank), 16, 16);
                var rankText = battle.Rank == 0 ? "Beginner" : string.Format(" > {0} hours", User.RankLimits[battle.Rank]);
                drawString("Minimum Rank: " + rankText);
                newLine();
            }

            if (founder.IsInGame)
            {
							drawImage(Resources.Boom, 16, 16);
                var timeString = DateTime.Now.Subtract(founder.InGameSince.Value).PrintTimeRemaining();
                drawString("The battle has been going on for at least " + timeString + ".");
                newLine();
            }
            if (battle.IsPassworded)
            {
							drawImage(Resources.Lock, 16, 16);
                drawString("Joining requires a password.");
                newLine();
            }

            if (battle.IsReplay)
            {
							drawImage(Resources.replay, 16, 16);
                drawString("Battle is replay.");
                newLine();
            }

            if (battle.IsLocked)
            {
                drawImage(Resources.Redlight, 16, 16);
                drawString("Battle is locked.");
                newLine();
            }
            newLine();


            foreach (var player in battle.Users)
            {
                var user = Program.TasClient.ExistingUsers[player.Name];
                var icon = TextImage.GetUserImage(user.Name);
                drawImage(icon, 16, 16);
                Image flag;
                y += 3;
                if (Images.CountryFlags.TryGetValue(user.Country, out flag) && flag != null) drawImage(flag, flag.Width, flag.Height);
                else x += 19;
                y -= 3;
                drawImage(Images.GetRank(user.Level), 16, 16);
                drawString(player.Name);

                var top10 = Program.SpringieServer.GetTop10Rank(user.Name);
                if (top10 > 0)
                {
                    var oldx = x;
										drawImage(Resources.Cup, 16, 16);
                    x -= 17;
                    drawString(top10.ToString());
                    x = oldx + 16;
                }

                if (!user.IsBot)
                {
                    if (user.IsAway) drawImage(Resources.AwayImage, 16, 16);
										if (user.IsInGame) drawImage(Resources.ingame, 16, 16);
                    if (user.Cpu==6666)
                    {
											drawImage(Resources.ZK_logo_square, 16, 16);
                    }
                }
                newLine();
            }
            if (Program.TasClient.MyBattle != null && battle.BattleID == Program.TasClient.MyBattle.BattleID && !String.IsNullOrEmpty(Program.ModStore.ChangedOptions))
            {
                newLine();
                drawString("Game Options:");
                newLine();
                foreach (var line in Program.ModStore.ChangedOptions.Lines().Where(z=>!string.IsNullOrEmpty(z)))
                {
                    drawString("  " + line);
                    newLine();
                }
            }

        }

        public Size? GetSize(Font font)
        {
            Battle battle;
            User founder;
            if (!GetBattleAndFounder(out battle, out founder)) return null;
            var h = 0;
            const int line = 16;
            h += line; // founder
            h += line; // map name
            h += line; // counts
            if (battle.IsPassworded) h += line;
            if (battle.IsReplay) h += line;
            if (battle.IsLocked) h += line;
            if (founder.IsInGame) h += line; // "battle has been going on for at least..."
            if (battle.Rank > 0) h += line;


            h += line; // blank line
            h += battle.Users.Count * line; // user names


            // mod options
            if (Program.TasClient.MyBattle != null && battle.BattleID == Program.TasClient.MyBattle.BattleID && !String.IsNullOrEmpty(Program.ModStore.ChangedOptions))
            {
                h += line; // blank line
                h += line; // title
                h += Program.ModStore.ChangedOptions.Lines().Where(x => !string.IsNullOrEmpty(x)).Count() * line;
            }

            h += 6; // margin
            return new Size(270,h);
        }


    }
}