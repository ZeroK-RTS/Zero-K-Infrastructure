using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using LobbyClient;
using PlasmaShared;
using ZkData;
using ZeroKLobby.MicroLobby;

namespace ZeroKLobby
{
    public class BattleTooltipRenderer:IToolTipRenderer
    {
        int battleID;

        public void SetBattleTooltipRenderer(int battleID)
        {
            this.battleID = battleID;
        }


        private bool GetBattle(out Battle battle)
        {
            //founder = null;
            if (Program.TasClient.ExistingBattles.TryGetValue(battleID, out battle)) {
                //founder = battle.Founder;
                return true;
            }
            return false;
        }

        public void Draw(Graphics g,  Font font, Color foreColor)
        {
            Battle battle;
            User founder;
            if (!GetBattle(out battle)) return;
            var fbrush = new SolidBrush(foreColor);
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
                    //y -= 3;
                    x += ToolTipHandler.TEXT_X_OFFSET;
                    TextRenderer.DrawText(g, text, font, new Point(x, y + ToolTipHandler.TEXT_Y_OFFSET), Config.TextColor, TextFormatFlags.LeftAndRightPadding);
                    x += TextRenderer.MeasureText(g, text, font).Width;
                    //y += 3;
                };
            Action<Image, int, int> drawImage = (image, w, h) =>
                {
                    g.DrawImage(image, x, y, w, h);
                    x += w + 3;
                };
            drawString("Founder: " + battle.FounderName);
            newLine();
            drawString("Map: " + battle.MapName);
            newLine();
            drawString("Players: " + battle.NonSpectatorCount);
            drawString("Spectators: " + battle.SpectatorCount);
            drawString("Friends: " + battle.Users.Count(u => Program.TasClient.Friends.Contains(u.Key)));
            newLine();


            if (battle.IsInGame)
            {
                drawImage(Buttons.fight, 16, 16);
                if (battle.RunningSince != null) {
                    var timeString = DateTime.UtcNow.Subtract(battle.RunningSince.Value).PrintTimeRemaining();
                    drawString("Battle running for " + timeString + ".");
                }
                newLine();
            }
            if (battle.IsPassworded)
            {
                drawImage(ZklResources._lock, 16, 16);
                drawString("Joining requires a password.");
                newLine();
            }

            newLine();


            foreach (var player in battle.Users.Values.Select(u=>u.LobbyUser))
            {
                var user = player;
                var icon = TextImage.GetUserImage(user.Name);
                drawImage(icon, 16, 16);
                Image flag;
                y += 3;
                if (Images.CountryFlags.TryGetValue(user.Country, out flag) && flag != null) drawImage(flag, flag.Width, flag.Height);
                else x += 19;
                y -= 3;
                if (!user.IsBot)
                {
                    drawImage(Images.GetRank(user.Level, user.EffectiveMmElo), 16, 16);
                    var clan = ServerImagesHandler.GetClanOrFactionImage(user);

                    if (clan.Item1 != null) drawImage(clan.Item1, 16, 16);
                }
                /*
                if (user.IsZkLobbyUser)
                {
                    drawImage(Resources.ZK_logo_square, 16, 16);
                }*/
                drawString(player.Name);


                if (!user.IsBot)
                {
                    if (user.IsAway) drawImage(ZklResources.away, 16, 16);
                    if (user.IsInGame) drawImage(Buttons.fight, 16, 16);
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
            fbrush.Dispose();
        }

        public Size? GetSize(Font font)
        {
            Battle battle;
            User founder;
            if (!GetBattle(out battle)) return null;
            var h = 0;
            const int line = 16;
            h += line; // founder
            h += line; // map name
            h += line; // counts
            if (battle.IsPassworded) h += line;
            if (battle.IsInGame) h += line; // "battle has been going on for at least..."


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