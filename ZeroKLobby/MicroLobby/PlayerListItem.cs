using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Web.Services.Description;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using LobbyClient;
using ZkData.UnitSyncLib;

namespace ZeroKLobby.MicroLobby
{
    public class PlayerListItem: IDisposable
    {
        /// <summary>
        /// Important values used to make sure playerlist shows name in correct order during searching and/or in battleroom
        /// </summary>
        public enum SortCats
        {
            SearchTitle = 0,
            SearchMatchedPlayer = 1,
            SearchNoMatchTitle =2,
            SearchNoMatchPlayer = 3,
            //others
            Uncategorized = 4,
            SpectatorTitle = 101,
            Spectators = 102,
        }
        readonly Font boldFont = new Font(Config.GeneralFont, FontStyle.Bold);

        Font font = Config.ChatFont;

        int height = 20;
        /// <summary>
        /// PlayerListItem will avoid using TasClient when this flag is true.
        /// </summary>
        public bool isOfflineMode = false;
        public bool isZK = false;
        public UserBattleStatus offlineUserBattleStatus;
        public User offlineUserInfo;
        public int? AllyTeam { get; set; }
        public BotBattleStatus BotBattleStatus { get; set; }
        public string Button { get; set; }
        public int Height { get { return height; } set { height = value; } }
        public bool IsGrayedOut { get; set; }
        public bool IsSpectatorsTitle { get; set; }
        public int SortCategory { get; set; }
        public string Title { get; set; }

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
                var bat = Program.TasClient.MyBattle;
                if (bat != null && UserName != null) {
                    UserBattleStatus ubs;
                    if (bat.Users.TryGetValue(UserName, out ubs)) return ubs;
                }
                return null;
            }
        }

        public string UserName { get; set; }


        public PlayerListItem()
        {
            SortCategory = (int)SortCats.Uncategorized;
        }

        ~PlayerListItem()
        {
            Dispose();
            GC.SuppressFinalize(this);
        }


        public void Dispose()
        {
            //font.Dispose();
            boldFont.Dispose();
        }

        public void DrawPlayerLine(Graphics g, Rectangle bounds, Color textColor,  bool grayedOut, bool isBattle)
        {
            g.TextRenderingHint = TextRenderingHint.SystemDefault;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            var x = 0;

            if (grayedOut) textColor = Program.Conf.FadeColor;

            Action<Image> drawImage = image =>
                {
                    g.DrawImage(image, bounds.Left + x, bounds.Top, 16, 16);
                    x += 19;
                };

            Action<string, Color> drawText = (text, color) =>
            {
                    TextRenderer.DrawText(g, text, font, new Point(bounds.Left + x, bounds.Top-2), color, TextFormatFlags.PreserveGraphicsTranslateTransform);
                    x += TextRenderer.MeasureText(g, text, font).Width;
                };

            // is section header
            if (Title != null)
            {
                font = boldFont;
                if (Title == "Search results:") drawImage(ZklResources.search);
                TextRenderer.DrawText(g, Title, font, bounds, textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.PreserveGraphicsTranslateTransform);
                return;
            }

            // is button
            if (Button != null)
            {
                font = boldFont;
                FrameBorderRenderer.Instance.RenderToGraphics(g, bounds, FrameBorderRenderer.StyleType.DarkHive);
                TextRenderer.DrawText(g, Button, font, bounds, textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.PreserveGraphicsTranslateTransform);
                return;
            }


            // is bot
            if (BotBattleStatus != null)
            {
                var bot = BotBattleStatus;
                x += 19;
                drawImage(ZklResources.robot);
                var botDisplayName = bot.aiLib;
                drawText(botDisplayName + " (" + bot.owner + ")", textColor);
                return;
            }

            // is player
            var user = User;
            var userStatus = UserBattleStatus;

            if (UserName != null && user == null)
            {
                drawText(UserName + " has left.", textColor);
                return;
            }

            if (isBattle)
            {
                if (userStatus.IsSpectator && (Program.TasClient.MyBattle == null || Program.TasClient.MyBattle.FounderName == userStatus.Name)) drawImage(ZklResources.spec);
                else if (userStatus.SyncStatus == SyncStatuses.Synced) drawImage(ZklResources.ready);
                else drawImage(ZklResources.unready);
            }

            drawImage(TextImage.GetUserImage(user.Name));

            Image flag;
            if (Images.CountryFlags.TryGetValue(user.Country, out flag) && flag != null) g.DrawImageUnscaled(flag, bounds.Left + x, bounds.Top + 4);
            x += 16;
            x += 2; // margin

            if (!user.IsBot)
            {
                drawImage(Images.GetRank(user.Level, user.EffectiveMmElo));

                var clan = ServerImagesHandler.GetClanOrFactionImage(user);
                if (clan.Item1 != null) drawImage(clan.Item1);
            }

            var userDisplayName = user.Name;
            drawText(userDisplayName, textColor);

            if (user.IsInGame) drawImage(Buttons.fight);
            else if (!isBattle && user.IsInBattleRoom) drawImage(Buttons.game);

            if (user.IsAway) drawImage(ZklResources.away);


            if (user.SteamID != null) {
                bool isEnabled;
                bool isTalking;
                Program.SteamHandler.Voice.GetUserVoiceInfo(ulong.Parse(user.SteamID), out isEnabled, out isTalking);
                if (isEnabled) {
                    drawImage(isTalking ? ZklResources.voice_talking : ZklResources.voice_off);
                }
            }
        }

        public string GetSortingKey()
        {
            if (SortCategory < (int)SortCats.Uncategorized)
                return SortCategory.ToString("00000") + UserName??string.Empty; //for ChatControl's search-filter only

            var name = string.Empty;

            if (UserBattleStatus != null)
            {
                name = UserBattleStatus.Name;
                if (UserBattleStatus.IsSpectator) SortCategory = (int)PlayerListItem.SortCats.Spectators;
                else SortCategory = UserBattleStatus.AllyNumber * 2 + 1 + (int)PlayerListItem.SortCats.Uncategorized;
            }
            else if (BotBattleStatus != null)
            {
                name = BotBattleStatus.Name;
                SortCategory = BotBattleStatus.AllyNumber * 2 + 1 + (int)PlayerListItem.SortCats.Uncategorized;
            }
            else if (UserName != null) name = UserName;
            else if (Title != null) name = Title;
            return SortCategory.ToString("00000") + name;
        }
    }
}