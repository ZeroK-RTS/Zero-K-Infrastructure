using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms.VisualStyles;
using ZkData;

namespace ZeroKLobby
{
    public class ButtonRenderer
    {
        static Size SizeMult(Size s, double factor)
        {
            return new Size((int)Math.Round(s.Width * factor), (int)Math.Round(s.Height * factor));
        }

        private static void FillRectangleTiled(Graphics g, TextureBrush brush, int x, int y, int w, int h)
        {
            brush.TranslateTransform(x, y);
            g.FillRectangle(brush, x, y, w, h);
        }

        public void RenderToGraphics(Graphics g, Rectangle r, ButtonStyle style)
        {
            TextureBrush northBrush;
            TextureBrush southBrush;
            TextureBrush eastBrush;
            TextureBrush westBrush;

            var nw = style.NW.Size;
            var ne = style.NE.Size;
            var n = style.N.Size;
            var sw = style.SW.Size;
            var se = style.SE.Size;
            var s = style.S.Size;
            var e = style.E.Size;
            var w = style.W.Size;

            var downScale = Math.Min(1.0, (double)r.Width / (nw.Width + ne.Width + n.Width));
            downScale = Math.Min(downScale, (double)r.Height / (nw.Height + sw.Height + w.Height));

            if (downScale < 1)
            {
                nw = SizeMult(nw, downScale);
                ne = SizeMult(ne, downScale);
                sw = SizeMult(sw, downScale);
                se = SizeMult(se, downScale);
                n = SizeMult(n, downScale);
                s = SizeMult(s, downScale);
                e = SizeMult(e, downScale);
                w = SizeMult(w, downScale);

                northBrush = new TextureBrush(style.N.GetResized(n.Width, n.Height, InterpolationMode.Default), WrapMode.TileFlipY);
                southBrush = new TextureBrush(style.S.GetResized(s.Width, s.Height, InterpolationMode.Default), WrapMode.TileFlipY);
                eastBrush = new TextureBrush(style.E.GetResized(e.Width, e.Height, InterpolationMode.Default), WrapMode.TileFlipX);
                westBrush = new TextureBrush(style.W.GetResized(w.Width, w.Height, InterpolationMode.Default), WrapMode.TileFlipX);
            }
            else
            {
                northBrush = new TextureBrush(style.N, WrapMode.TileFlipY);
                southBrush = new TextureBrush(style.S, WrapMode.TileFlipY);
                eastBrush = new TextureBrush(style.E, WrapMode.TileFlipX);
                westBrush = new TextureBrush(style.W, WrapMode.TileFlipX);
            }


            using (northBrush)
            using (southBrush)
            using (eastBrush)
            using (westBrush)
            {

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

                var tl = SizeMult(style.FillOffsetTopLeft, downScale);
                var br = SizeMult(style.FillOffsetBottomRight, downScale);

                g.FillRectangle(final, nw.Width + tl.Width, nw.Height + tl.Height,
                    r.Width - nw.Width - ne.Width - 1 - br.Width - tl.Width,
                    r.Height - sw.Height - nw.Height - 1 - br.Height - tl.Height);

                if (final != style.FillBrush) final.Dispose();
            }


        }


    }
}