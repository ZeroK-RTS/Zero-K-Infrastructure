using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;
using PlasmaShared;
using SpringDownloader;

namespace SpringDownloader.MicroLobby
{
    public class GameSelector: ScrollableControl
    {
        const int buttonAntialiasing = 3;
        const string joinText =
            "SpringDownloader will join a battle for you and take care of everything you need to play, including getting the required files and finding a battle.";
        const string watchText = "Watch other people play.";
        bool areButtonsDirty = true;
        Image button1;
        Image button2;
        readonly Font buttonFont;
        int buttonHeight;
        int buttonWidth;
        readonly List<GameIcon> gameIcons;
        GameIcon hoverGameIcon;
        int iconHeight;
        int iconWidth;
        bool joinHover;
        Point mouseLocation;
        readonly List<GameIcon> selectedIcons;
        bool watchButtonHover;

        public IEnumerable<GameInfo> Games { get; set; }

        public GameSelector(IEnumerable<GameInfo> games)
        {
            Games = games;
            AutoScroll = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
            gameIcons = StartPage.GameList.Select(game => new GameIcon(game)).Shuffle();

            selectedIcons = gameIcons.Where(x => Program.Conf.SelectedGames.Contains(x.GameInfo.Shortcut)).ToList();

            BackColor = Color.White;

            var family = FontFamily.Families.FirstOrDefault(f => f.Name.Contains("Segoe UI Semibold")) ?? FontFamily.GenericSansSerif;
            Font = new Font(family, 10, FontStyle.Regular);
            buttonFont = new Font(family, 20*buttonAntialiasing);
        }


        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            var gameIcon = GetGameIcon(e.X, e.Y);
            if (gameIcon != null)
            {
                var gameName = gameIcon.GameInfo.Shortcut;
                if (selectedIcons.Contains(gameIcon))
                {
                    selectedIcons.Remove(gameIcon);
                    ActionHandler.DeselectGame(gameName);
                }
                else
                {
                    selectedIcons.Add(gameIcon);
                    ActionHandler.SelectGame(gameName);
                }
                Invalidate();
            }
            var button1Rect = new Rectangle(buttonWidth, buttonHeight/3, buttonWidth, buttonHeight);
            if (button1Rect.Contains(e.Location)) ActionHandler.StartQuickMatching(selectedIcons.Select(i => i.GameInfo));
            var button2Rect = new Rectangle(buttonWidth*3, buttonHeight/3, buttonWidth, buttonHeight);
            if (button2Rect.Contains(e.Location))
            {
                ActionHandler.StartQuickMatching(selectedIcons.Select(i => i.GameInfo));
                ActionHandler.ChangeDesiredSpectatorState(true);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var gameIcon = GetGameIcon(e.X, e.Y);
            var button1Rect = new Rectangle(buttonWidth, buttonHeight/3, buttonWidth, buttonHeight);
            var button2Rect = new Rectangle(buttonWidth*3, buttonHeight/3, buttonWidth, buttonHeight);

            joinHover = button1Rect.Contains(e.Location);
            watchButtonHover = button2Rect.Contains(e.Location);

            if (gameIcon != null || joinHover || watchButtonHover) Cursor = Cursors.Hand;
            else Cursor = Cursors.Default;

            var cursorPoint = new Point(e.X, e.Y);
            if (cursorPoint == mouseLocation) return;
            mouseLocation = cursorPoint;

            if (joinHover || watchButtonHover) Invalidate();

            if (hoverGameIcon != gameIcon)
            {
                hoverGameIcon = gameIcon;
                Invalidate();
            }
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            try
            {
                base.OnPaint(e);
                e.Graphics.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);

                var x = 0;
                var y = 0;

                buttonWidth = Width/5;
                buttonHeight = (int)(buttonWidth/1.62/2);

                if (areButtonsDirty)
                {
                    button1.SafeDispose();
                    button2.SafeDispose();
                    button1 = MakeButton("Join a Battle", buttonWidth, buttonHeight);
                    button2 = MakeButton("Watch a Battle", buttonWidth, buttonHeight);
                    areButtonsDirty = false;
                }

                y += buttonHeight/3;

                e.Graphics.DrawImageUnscaled(button1, x + buttonWidth, y);
                e.Graphics.DrawImageUnscaled(button2, x + buttonWidth*3, y);

                y += buttonHeight/3*2 + buttonHeight;

                const int columns = 4;
                const int rows = 2;

                var descriptionAreaSize = new Size(Width*2/3, 80);

                var maxIconHeight = (Height - descriptionAreaSize.Height - buttonHeight/3 - buttonHeight/3*2 - buttonHeight)/rows;
                var maxIconWidth = Width/columns;

                if (maxIconHeight*1.62 > maxIconWidth)
                {
                    iconWidth = maxIconWidth;
                    iconHeight = (int)(iconWidth/1.62);
                }
                else
                {
                    iconHeight = maxIconHeight;
                    iconWidth = (int)(maxIconHeight*1.62);
                }

                var xStart = Width/2 - iconWidth*columns/2;
                x = xStart;
                for (var i = 0; i < gameIcons.Count; i++)
                {
                    if (i%columns == 0 && i != 0)
                    {
                        x = xStart;
                        y += iconHeight;
                    }
                    var icon = gameIcons[i];
                    var isHovered = icon.HitTest(mouseLocation.X, mouseLocation.Y);
                    var image = icon.GetImage(isHovered, x, y, iconWidth, selectedIcons.Contains(icon));
                    e.Graphics.DrawImageUnscaled(image, x, y);
                    x += iconWidth;
                }

                y += iconHeight;

                if (hoverGameIcon != null || joinHover || watchButtonHover)
                {
                    var rect = new Rectangle(Width/2 - descriptionAreaSize.Width/2, y, descriptionAreaSize.Width, descriptionAreaSize.Height);
                    e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                    var text = hoverGameIcon != null ? hoverGameIcon.GameInfo.Description : watchButtonHover ? watchText : joinText;
                    using (var format = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center }) e.Graphics.DrawString(text, Font, Brushes.Black, rect, format);
                }
                y += descriptionAreaSize.Height;

                AutoScrollMinSize = new Size(0, y);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error drawing mod logo: " + ex);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            areButtonsDirty = true;
        }

        GameIcon GetGameIcon(int x, int y)
        {
            x -= AutoScrollPosition.X;
            y -= AutoScrollPosition.Y;
            return gameIcons.FirstOrDefault(i => i.HitTest(x, y));
        }

        Image MakeButton(string text, int buttonWidth, int buttonHeight)
        {
            using (var bitmap = new Bitmap(buttonWidth*buttonAntialiasing, buttonHeight*buttonAntialiasing))
            {
                using (var g = Graphics.FromImage(bitmap))
                {
                    var rect = new Rectangle(0, 0, buttonWidth*buttonAntialiasing, buttonHeight*buttonAntialiasing);
                    var region = Images.GetRoundedRegion(20*buttonAntialiasing, rect);
                    using (var backgrounBrush = new LinearGradientBrush(rect, GameIcon.Color1, GameIcon.Color2, 60.0f)) g.FillRegion(backgrounBrush, region);
                    g.TextRenderingHint = TextRenderingHint.SingleBitPerPixel;
                    using (var format = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center }) g.DrawString(text, buttonFont, GameIcon.TextBrush, rect, format);
                }
                try
                {
                    return bitmap.GetResized(buttonWidth, buttonHeight, InterpolationMode.Bilinear);
                } catch
                {
                    bitmap.Dispose();
                    throw;
                }
            }
        }
    }
}