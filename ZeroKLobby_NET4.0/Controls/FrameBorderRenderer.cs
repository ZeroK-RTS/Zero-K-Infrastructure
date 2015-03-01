using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using ZkData;

namespace ZeroKLobby
{
    public class FrameBorderRenderer
    {
        public enum StyleType
        {
            DarkHive,
            DarkHiveHover,
            Shraka,
            DarkHiveGlow,
            TechPanel
        }

        public class FrameStyle
        {
            public Image NW;
            public Image NE;
            public Image N;
            public Image SW;
            public Image SE;
            public Image S;
            public Image E;
            public Image W;

            public Size FillOffsetTopLeft = Size.Empty;
            public Size FillOffsetBottomRight = Size.Empty;
            public Brush FillBrush = Brushes.Transparent;

            public InterpolationMode Interpolation = InterpolationMode.HighQualityBicubic;
        }


        readonly Dictionary<CacheKey, Image> cachedImages = new Dictionary<CacheKey, Image>();
        public static FrameBorderRenderer Instance = new FrameBorderRenderer();
        public static Dictionary<StyleType, FrameStyle> Styles = new Dictionary<StyleType, FrameStyle> {
            {
                StyleType.DarkHiveHover,
                new FrameStyle {
                    N = DarkHiveHover.N,
                    NE = DarkHiveHover.NE,
                    NW = DarkHiveHover.NW,
                    S = DarkHiveHover.S,
                    SE = DarkHiveHover.SE,
                    SW = DarkHiveHover.SW,
                    E = DarkHiveHover.E,
                    W = DarkHiveHover.W,
                    FillBrush = new SolidBrush(Color.FromArgb(89, 23, 252, 255))
                }
            }, {
                StyleType.DarkHive,
                new FrameStyle {
                    N = DarkHive.N,
                    NE = DarkHive.NE,
                    NW = DarkHive.NW,
                    S = DarkHive.S,
                    SE = DarkHive.SE,
                    SW = DarkHive.SW,
                    E = DarkHive.E,
                    W = DarkHive.W,
                    FillBrush = new SolidBrush(Color.FromArgb(179, 0, 0, 0))
                }}, {
                StyleType.DarkHiveGlow,
                new FrameStyle {
                    N = DarkHiveGlow.N,
                    NE = DarkHiveGlow.NE,
                    NW = DarkHiveGlow.NW,
                    S = DarkHiveGlow.S,
                    SE = DarkHiveGlow.SE,
                    SW = DarkHiveGlow.SW,
                    E = DarkHiveGlow.E,
                    W = DarkHiveGlow.W,
                    FillBrush = new SolidBrush(Color.FromArgb(179, 0, 0, 0))
                }},
                {
                StyleType.TechPanel,
                new FrameStyle {
                    N = TechPanel.N,
                    NE = TechPanel.NE,
                    NW = TechPanel.NW,
                    S = TechPanel.S,
                    SE = TechPanel.SE,
                    SW = TechPanel.SW,
                    E = TechPanel.E,
                    W = TechPanel.W,
                    FillBrush = new SolidBrush(Color.FromArgb(255, 19, 65, 73)),
                    Interpolation = InterpolationMode.NearestNeighbor
                 }},
                {
                StyleType.Shraka,
                new FrameStyle {
                    N = Shraka.N,
                    NE = Shraka.NE,
                    NW = Shraka.NW,
                    S = Shraka.S,
                    SE = Shraka.SE,
                    SW = Shraka.SW,
                    E = Shraka.E,
                    W = Shraka.W,
                    FillBrush =
                        new LinearGradientBrush(new Rectangle(0, 0, 1, 1), Color.FromArgb(166, 0, 44, 61), Color.FromArgb(166, 0, 104, 141), 90),
                    FillOffsetTopLeft = new Size(-162, -78),
                    FillOffsetBottomRight = new Size(-174, -98)
                }
            }
        };


        public Image GetImageWithCache(Rectangle r, StyleType style, StyleType? overlayStyle = null)
        {
            var key = new CacheKey { OverlayStyle = overlayStyle, Rectangle = r, Style = style };

            Image img;
            if (cachedImages.TryGetValue(key, out img)) return img;
            var bmp = new Bitmap(r.Width, r.Height);
            using (Graphics g = Graphics.FromImage(bmp)) {
                RenderToGraphics(g, r, style);
                if (overlayStyle != null) RenderToGraphics(g, r, overlayStyle.Value);
            }

            cachedImages[key] = bmp;
            return bmp;
        }

        public void RenderToGraphics(Graphics g, Rectangle r, StyleType styleType)
        {
            FrameStyle style = Styles[styleType];
            TextureBrush northBrush;
            TextureBrush southBrush;
            TextureBrush eastBrush;
            TextureBrush westBrush;

            Size nw = style.NW.Size;
            Size ne = style.NE.Size;
            Size n = style.N.Size;
            Size sw = style.SW.Size;
            Size se = style.SE.Size;
            Size s = style.S.Size;
            Size e = style.E.Size;
            Size w = style.W.Size;

            double downScale = Math.Min(1.0, (double)r.Width/(nw.Width + ne.Width + n.Width));
            downScale = Math.Min(downScale, (double)r.Height/(nw.Height + sw.Height + w.Height));

            if (downScale < 1) {
                nw = SizeMult(nw, downScale);
                ne = SizeMult(ne, downScale);
                sw = SizeMult(sw, downScale);
                se = SizeMult(se, downScale);
                n = SizeMult(n, downScale);
                s = SizeMult(s, downScale);
                e = SizeMult(e, downScale);
                w = SizeMult(w, downScale);

                northBrush = new TextureBrush(style.N.GetResizedWithCache(n.Width, n.Height, style.Interpolation), WrapMode.TileFlipY);
                southBrush = new TextureBrush(style.S.GetResizedWithCache(s.Width, s.Height, style.Interpolation), WrapMode.TileFlipY);
                eastBrush = new TextureBrush(style.E.GetResizedWithCache(e.Width, e.Height, style.Interpolation), WrapMode.TileFlipX);
                westBrush = new TextureBrush(style.W.GetResizedWithCache(w.Width, w.Height, style.Interpolation), WrapMode.TileFlipX);
            } else {
                northBrush = new TextureBrush(style.N, WrapMode.TileFlipY);
                southBrush = new TextureBrush(style.S, WrapMode.TileFlipY);
                eastBrush = new TextureBrush(style.E, WrapMode.TileFlipX);
                westBrush = new TextureBrush(style.W, WrapMode.TileFlipX);
            }

            using (northBrush)
            using (southBrush)
            using (eastBrush)
            using (westBrush) {
                g.InterpolationMode = style.Interpolation;
                g.DrawImage(style.NW, 0, 0, nw.Width, nw.Height);
                g.DrawImage(style.NE, r.Width - ne.Width - 1, 0, ne.Width, ne.Height);
                g.DrawImage(style.SE, r.Width - se.Width - 1, r.Height - se.Height - 1, se.Width, se.Height);
                g.DrawImage(style.SW, 0, r.Height - sw.Height - 1, sw.Width, sw.Height);

                FillRectangleTiled(g, northBrush, nw.Width, 0, r.Width - nw.Width - ne.Width - 1, n.Height);
                FillRectangleTiled(g, southBrush, sw.Width, r.Height - s.Height - 1, r.Width - sw.Width - se.Width - 1, s.Height);
                FillRectangleTiled(g, westBrush, 0, nw.Height, w.Width, r.Height - nw.Height - sw.Height - 1);
                FillRectangleTiled(g, eastBrush, r.Width - e.Width - 1, ne.Height, e.Width, r.Height - ne.Height - se.Height - 1);

                Brush final;
                if (style.FillBrush is LinearGradientBrush) {
                    final = (LinearGradientBrush)style.FillBrush.Clone(); // linear gradient, adjust scaling
                    ((LinearGradientBrush)final).ScaleTransform(r.Height, r.Width);
                } else final = style.FillBrush;

                Size tl = SizeMult(style.FillOffsetTopLeft, downScale);
                Size br = SizeMult(style.FillOffsetBottomRight, downScale);

                g.FillRectangle(final, nw.Width + tl.Width, nw.Height + tl.Height, r.Width - nw.Width - ne.Width - 1 - br.Width - tl.Width,
                    r.Height - sw.Height - nw.Height - 1 - br.Height - tl.Height);

                if (final != style.FillBrush) final.Dispose();
            }
        }

        static void FillRectangleTiled(Graphics g, TextureBrush brush, int x, int y, int w, int h)
        {
            brush.TranslateTransform(x, y);
            g.FillRectangle(brush, x, y, w, h);
        }

        static Size SizeMult(Size s, double factor)
        {
            return new Size((int)Math.Round(s.Width*factor), (int)Math.Round(s.Height*factor));
        }

        /// <summary>
        /// Key for caching images (defines style and size parameters)
        /// </summary>
        public class CacheKey
        {
            public StyleType? OverlayStyle;
            public Rectangle Rectangle;
            public StyleType Style;

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((CacheKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked {
                    int hashCode = Rectangle.GetHashCode();
                    hashCode = (hashCode*397) ^ (int)Style;
                    hashCode = (hashCode*397) ^ OverlayStyle.GetHashCode();
                    return hashCode;
                }
            }

            protected bool Equals(CacheKey other)
            {
                return Rectangle.Equals(other.Rectangle) && Style == other.Style && OverlayStyle == other.OverlayStyle;
            }
        }
    }
}