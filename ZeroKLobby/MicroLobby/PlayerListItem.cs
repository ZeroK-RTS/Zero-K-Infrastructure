using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using LobbyClient;
using PlasmaShared.LobbyMessages;
using ZkData.UnitSyncLib;

namespace ZeroKLobby.MicroLobby
{
    public class PlayerListItem: IDisposable
    {
        //readonly Font boldFont = new Font("Microsoft Sans Serif", 9, FontStyle.Bold);
        readonly Font boldFont = new Font(Program.Conf.ChatFont.FontFamily, Program.Conf.ChatFont.Size - 2, FontStyle.Bold);

        //Font font = new Font("Segoe UI", 9, FontStyle.Regular);
        Font font = new Font(Program.Conf.ChatFont.FontFamily, Program.Conf.ChatFont.Size - 2, FontStyle.Regular);

        int height = 16;
        /// <summary>
        /// PlayerListItem will avoid using TasClient when this flag is true.
        /// </summary>
        public bool isOfflineMode = false;
        public bool isZK = false;
        public UserBattleStatus offlineUserBattleStatus;
        public User offlineUserInfo;
        /// <summary>
        /// PlayerListItem will never render or draw any content when this flag is true.
        /// </summary>
        public bool isDummy = false;
        public int? AllyTeam { get; set; }
        public BotBattleStatus BotBattleStatus { get; set; }
        public string Button { get; set; }
        public int Height { get { return height; } set { height = value; } }
        public bool IsGrayedOut { get; set; }
        public bool IsSpectatorsTitle { get; set; }
        public MissionSlot MissionSlot { get; set; }
        public string SlotButton;
        public int SortCategory { get; set; }
        public string Title { get; set; }
        public bool IsZeroKBattle 
        {
            get
            {
                if (isOfflineMode) return isZK;
                return Program.TasClient.MyBattle != null && KnownGames.GetGame(Program.TasClient.MyBattle.ModName) != null 
                    && KnownGames.GetGame(Program.TasClient.MyBattle.ModName).IsPrimary;
            } 
        }
        public bool IsSpringieBattle { get { return Program.TasClient.MyBattle != null && Program.TasClient.MyBattle.Founder.IsZkLobbyUser; } }
        public User User
        {
            get
            {
                if (UserName == null) return null;
                if (isOfflineMode) return offlineUserInfo;
                User user;
                Program.TasClient.ExistingUsers.TryGetValue(UserName, out user);
                return user;
            }
        }
        public UserBattleStatus UserBattleStatus
        {
            get 
            {
                if (isOfflineMode) return offlineUserBattleStatus;
                return Program.TasClient.MyBattle == null ? null : Program.TasClient.MyBattle.Users.SingleOrDefault(u => u.Name == UserName);
            }
        }

        public string UserName { get; set; }


        public PlayerListItem()
        {
            height = (int)font.Size*2;
        }

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
            if (isDummy)
                return;

            g.TextRenderingHint = TextRenderingHint.SystemDefault;
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
                if (Title == "Search results:") drawImage(ZklResources.search);
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

            // is player slot
            if (SlotButton != null)
            {
                var slotText = string.Format("Empty Slot: {0} {1}", MissionSlot.TeamName, MissionSlot.IsRequired ? "(Required)" : String.Empty);
                var color = ((MyCol)MissionSlot.Color);
                if (!IsZeroKBattle)
                {
                    using (var brush = new SolidBrush(Color.FromArgb(color.R, color.G, color.B)))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.FillEllipse(brush, x, bounds.Top, bounds.Bottom - bounds.Top, bounds.Bottom - bounds.Top);
                    }
                }
                x += bounds.Bottom - bounds.Top + 2;
                drawText(slotText, foreColor, backColor);
                return;
            }

            // is bot
            if (BotBattleStatus != null)
            {
                var bot = BotBattleStatus;
                x += 19;
                var botColor = BotBattleStatus.TeamColorRGB;
                if (!IsZeroKBattle)
                {
                    using (var brush = new SolidBrush(Color.FromArgb(botColor[0], botColor[1], botColor[2])))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.FillEllipse(brush, x, bounds.Top, bounds.Bottom - bounds.Top, bounds.Bottom - bounds.Top);
                    }
                }
                drawImage(ZklResources.robot);
                var botDisplayName = MissionSlot == null ? bot.aiLib : MissionSlot.TeamName;
                drawText(botDisplayName + " (" + bot.owner + ")", foreColor, backColor);
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
                if (userStatus.IsSpectator && (Program.TasClient.MyBattle == null || !Program.TasClient.MyBattle.IsQueue || Program.TasClient.MyBattle.Founder.Name == userStatus.Name)) drawImage(ZklResources.spec);
                else if (userStatus.SyncStatus == SyncStatuses.Synced && (IsSpringieBattle || userStatus.IsReady)) drawImage(ZklResources.ready);
                else drawImage(ZklResources.unready);

                if (!userStatus.IsSpectator)
                {
                    if (!IsZeroKBattle)
                    {
                        var userColor = userStatus.TeamColorRGB;
                        using (var brush = new SolidBrush(Color.FromArgb(userColor[0], userColor[1], userColor[2])))
                        {
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            g.FillEllipse(brush, x, bounds.Top, bounds.Bottom - bounds.Top, bounds.Bottom - bounds.Top);
                        }
                    }
                }
            }

            drawImage(TextImage.GetUserImage(user.Name));

            Image flag;
            if (Images.CountryFlags.TryGetValue(user.Country, out flag) && flag != null) g.DrawImageUnscaled(flag, bounds.Left + x, bounds.Top + 4);
            x += 16;
            x += 2; // margin

            if (!user.IsBot)
            {
                drawImage(Images.GetRank(user.Level));

                var clan = ServerImagesHandler.GetClanOrFactionImage(user);
                if (clan.Item1 != null) drawImage(clan.Item1);
            }

            var userDisplayName = MissionSlot == null ? user.Name : String.Format("{1}: {0}", MissionSlot.TeamName, user.Name);
            drawText(userDisplayName, foreColor, backColor);
            var top10 = Program.SpringieServer.GetTop10Rank(user.Name);
            if (top10 > 0)
            {
                var oldProgression = x;
                drawImage(ZklResources.cup);
                x = oldProgression;
                TextRenderer.DrawText(g, top10.ToString(), boldFont, new Point(bounds.Left + x + 1, bounds.Top), Color.Black, Color.Transparent);
                x += 16;
            }

            if (user.IsInGame) drawImage(ZklResources.ingame);
            else if (!isBattle && user.IsInBattleRoom) drawImage(ZklResources.battle);

            if (user.IsAway) drawImage(ZklResources.away);

            if (isBattle && !userStatus.IsSpectator)
                if (MissionSlot != null)
                    if (userStatus.AllyNumber != MissionSlot.AllyID)
                        drawText(string.Format("Wrong alliance ({0} instead of {1}).", userStatus.AllyNumber, MissionSlot.AllyID),
                                 Color.Red,
                                 backColor);

            if (user.SteamID != null) {
                bool isEnabled;
                bool isTalking;
                Program.SteamHandler.Voice.GetUserVoiceInfo(user.SteamID.Value, out isEnabled, out isTalking);
                if (isEnabled) {
                    drawImage(isTalking ? ZklResources.voice_talking : ZklResources.voice_off);
                }
            }
        }

        public override string ToString()
        {
            var name = string.Empty;
            if (MissionSlot != null)
            {
                SortCategory = MissionSlot.TeamID;
                name = MissionSlot.TeamName;
            }
            else if (UserBattleStatus != null)
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