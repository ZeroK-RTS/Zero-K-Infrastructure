using System.Drawing;
using System.Drawing.Drawing2D;

namespace ZeroKLobby.MicroLobby
{
    class BattleGameListItem: GdiListBoxItem
    {
        new const int height = 45;
        const double innerRatio = 0.9;
        static Bitmap box;
        protected static Brush brush;
        static Font font;
        static Bitmap selectedBox;
        protected string text;
        public readonly GameInfo Game;

        public bool IsSelected { get; set; }

        static BattleGameListItem()
        {
            var color2 = Color.FromArgb(48, 51, 255);
            var color1 = Color.FromArgb(48, 175, 255);
            selectedBox = GenerateBox(color1, color2);
            box = GenerateBox(GameIcon.Color1, GameIcon.Color2);
            font = new Font("Segoe UI", 10f, FontStyle.Bold);
            brush = GameIcon.TextBrush;
        }

        public BattleGameListItem()
        {
            base.height = height;
        }

        public BattleGameListItem(GameInfo game): this()
        {
            Game = game;
            text = game.FullName;
        }

        public override void Draw(Graphics g, ref int y, int scrollPosition)
        {
            this.y = y + scrollPosition;
            g.DrawImageUnscaled(GetBox(), 0, y);
            var rect = new Rectangle(0, y + (int)(height*(1 - innerRatio)/2), box.Width, box.Height);
            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center }) g.DrawString(text, font, brush, rect, format);
            y += height;
        }

        protected static Bitmap GenerateBox(Color color1, Color color2)
        {
            const int antialias = 3;
            const int width = BattleGameList.Width;
            const int aaWidth = width*antialias;
            const int aaHeight = height*antialias;
            const int finalInnerHeight = (int)(height*innerRatio);
            const int aaInnerHeight = finalInnerHeight*antialias;
            using (var aaImage = new Bitmap(aaWidth, aaHeight))
            {
                using (var g = Graphics.FromImage(aaImage))
                {
                    const int left = 0;
                    const int top = (aaHeight - aaInnerHeight)/2;
                    var rectangle = new Rectangle(left, top, aaWidth, aaInnerHeight);
                    var region = Images.GetRoundedRegion(10*antialias, rectangle);
                    using (var backgrounBrush = new LinearGradientBrush(rectangle, color1, color2, 60.0f)) g.FillRegion(backgrounBrush, region);
                }
                return aaImage.GetResized(width, height, InterpolationMode.Bilinear);
            }
        }

        protected virtual Image GetBox()
        {
            return IsSelected ? selectedBox : box;
        }
    }
}