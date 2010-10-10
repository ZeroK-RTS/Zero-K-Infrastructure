using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using PlasmaShared;

namespace SpringDownloader.MicroLobby
{
    class GameIcon : IDisposable
    {
        public static readonly Color Color1 = Color.FromArgb(255, 30, 0);
        public static readonly Color Color2 = Color.FromArgb(150, 15, 0);
        public static readonly Font Font;
        const double innerBoxHoverRatio = 1;
        const double innerBoxNormalRatio = 0.9;
        public static readonly Brush TextBrush;
        public static readonly Color TextColor = Color.FromArgb(255, 255, 160);
        static Image background;
        int border = 15;
        Image grayImage;
        Image GrayImage
        {
            get
            {
                if (grayImage == null) grayImage = Images.GetGrayScaleImage(image);
                return grayImage;
            }
        }
        int height;
        static Image hoverBackground;
        bool hovering;
        Image image;
        double innerBoxSizeRatio = 0.9;
        bool isDirty = true;
        int width;
        int x;
        int y;
        public GameInfo GameInfo { get; set; }


        static GameIcon()
        {
            TextBrush = new SolidBrush(TextColor);
            var family = FontFamily.Families.FirstOrDefault(f => f.Name.Contains("Segoe UI Semibold")) ?? FontFamily.GenericSansSerif;
            Font = new Font(family, 10, FontStyle.Regular);
        }

        public GameIcon(GameInfo gameInfo)
        {
            GameInfo = gameInfo;
        }

        public static int GetHeight(int iconWidth)
        {
            return Math.Max((int)(iconWidth/1.62), 1);
        }

        public Image GetImage(bool hovering, int x, int y, int width, bool selected)
        {
            this.x = x;
            this.y = y;
            if (hovering != this.hovering || width != this.width)
            {
                this.hovering = hovering;
                this.width = Math.Max(1, width);
                isDirty = true;
            }
            GenerateIcon();
            return selected ? image : GrayImage;
        }

        public bool HitTest(int x, int y)
        {
            x = x - this.x;
            y = y - this.y;
            return x > width*(1 - innerBoxSizeRatio)/2 && x < width*(1 - (1 - innerBoxSizeRatio)/2) && y > height*(1 - innerBoxSizeRatio)/2 &&
                   y < height*(1 - (1 - innerBoxSizeRatio)/2);
        }


        static void GenerateBackground(int width, int height)
        {
            if (background == null || width != background.Width || height != background.Height)
            {
                background.SafeDispose();
                hoverBackground.SafeDispose();
                background = GenerateBox(width, height, innerBoxNormalRatio);
                hoverBackground = GenerateBox(width, height, innerBoxHoverRatio);
            }
        }

        static Bitmap GenerateBox(int width, int height, double innerRatio)
        {
            const int antialias = 3;

            var aaWidth = width*antialias;
            var aaHeight = height*antialias;

            var finalInnerWidth = (int)(width*innerRatio);
            var finalInnerHeight = (int)(height*innerRatio);

            var aaInnerWidth = finalInnerWidth*antialias;
            var aaInnerHeight = finalInnerHeight*antialias;

            using (var aaImage = new Bitmap(aaWidth, aaHeight))
            {
                using (var g = Graphics.FromImage(aaImage))
                {
                    var left = (aaWidth - aaInnerWidth)/2;
                    var top = (aaHeight - aaInnerHeight)/2;
                    var rectangle = new Rectangle(left, top, Math.Max(aaInnerWidth, 1), Math.Max(aaInnerHeight, 1));
                    var region = Images.GetRoundedRegion(20*antialias, rectangle);
                    using (var backgrounBrush = new LinearGradientBrush(rectangle, Color1, Color2, 60.0f)) g.FillRegion(backgrounBrush, region);
                }
                return aaImage.GetResized(width, height, InterpolationMode.Bilinear);
            }
        }

        void GenerateIcon()
        {
            if (!isDirty) return;
            isDirty = false;

            if (hovering) innerBoxSizeRatio = innerBoxHoverRatio;
            else innerBoxSizeRatio = innerBoxNormalRatio;

            height = GetHeight(width);
            GenerateBackground(width, height);

            grayImage.SafeDispose();
            grayImage = null;
            image.SafeDispose();

            image = new Bitmap(hovering ? hoverBackground : background);

            using (var g = Graphics.FromImage(image))
            {
                var innerWidth = (int)(width*innerBoxSizeRatio);
                var innerHeight = (int)(height*innerBoxSizeRatio);
                var left = (width - innerWidth)/2;
                var top = (height - innerHeight)/2;
                var extra = hovering ? 15 : 0;
                g.DrawImage(GameInfo.Logo, left + border, top + border + extra, innerWidth - border*2, innerHeight - border*2 - extra);
                if (hovering)
                {
                    g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                    var layoutRectangle = new Rectangle(left, top, innerWidth, border + extra);
                    using (var format = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center }) g.DrawString(GameInfo.FullName, Font, TextBrush, layoutRectangle, format);
                }
            }
        }

        public void Dispose()
        {
            image.SafeDispose();
            grayImage.SafeDispose();
        }
    }
}