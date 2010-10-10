using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Windows;

namespace SpringDownloader.MicroLobby
{
    static class Images
    {
        static Assembly assembly;
        static Image[] rankImages = new Image[9];

        public static Dictionary<string, Image> CountryFlags = new Dictionary<string, Image>();


        static Images()
        {
            assembly = Assembly.GetAssembly(typeof(Images));
            foreach (var country in LobbyClient.User.CountryNames.Keys) CountryFlags[country] = (Image)Flags.ResourceManager.GetObject(country.ToLower());
            for (var i = 0; i < 9; i++) rankImages[i] = (Image)Ranks.ResourceManager.GetObject(string.Format("_{0}",i+1));
        }

        public static Bitmap AdjustBrightness(this Image Image, Color contrastMultiplier, Color brightnessBonus)
        {
            var TempBitmap = Image;
            var NewBitmap = new Bitmap(TempBitmap.Width, TempBitmap.Height);
            var NewGraphics = Graphics.FromImage(NewBitmap);
            float[][] FloatColorMatrix = {
                                             new[] { contrastMultiplier.R/255.0f, 0, 0, 0, 0 }, new[] { 0, contrastMultiplier.G/255.0f, 0, 0, 0 },
                                             new[] { 0, 0, contrastMultiplier.B/255.0f, 0, 0 }, new float[] { 0, 0, 0, 1, 0 },
                                             new[] { brightnessBonus.R/255.0f, brightnessBonus.G/255.0f, brightnessBonus.B/255.0f, 1, 1 }
                                         };

            var NewColorMatrix = new ColorMatrix(FloatColorMatrix);
            var Attributes = new ImageAttributes();
            Attributes.SetColorMatrix(NewColorMatrix);
            NewGraphics.DrawImage(TempBitmap,
                                  new Rectangle(0, 0, TempBitmap.Width, TempBitmap.Height),
                                  0,
                                  0,
                                  TempBitmap.Width,
                                  TempBitmap.Height,
                                  GraphicsUnit.Pixel,
                                  Attributes);
            Attributes.Dispose();
            NewGraphics.Dispose();
            return NewBitmap;
        }

        public static void DrawStringWithOutline(this Graphics g,
                                                 string text,
                                                 Font font,
                                                 Brush brush,
                                                 Brush outlineBrush,
                                                 Rectangle layoutRect,
                                                 StringFormat format,
                                                 int outlineSize)
        {
            for (var x = 0; x < outlineSize; x++)
            {
                for (var y = 0; y < outlineSize; y++)
                {
                    var rect = new Rectangle(layoutRect.X + x - outlineSize/2, layoutRect.Y + y - outlineSize/2, layoutRect.Width, layoutRect.Height);
                    g.DrawString(text, font, outlineBrush, rect, format);
                }
            }

            g.DrawString(text, font, brush, layoutRect, format);
        }


        /// <summary>
        /// This method draws a grayscale image from a given Image-
        /// instance and gives back the Bitmap of it.
        /// </summary>
        /// <param name="img">the source-image</param>
        /// <returns>Bitmap-Object with grayscale image</returns>
        public static Bitmap GetGrayScaleImage(Image img)
        {
            var grayBitmap = new Bitmap(img.Width, img.Height);

            try
            {
                using (var imgAttributes = new ImageAttributes()) {
                    var gray =
                        new ColorMatrix(new[]
                                        {
                                            new[] { 0.299f, 0.299f, 0.299f, 0, 0 }, new[] { 0.588f, 0.588f, 0.588f, 0, 0 },
                                            new[] { 0.111f, 0.111f, 0.111f, 0, 0 }, new float[] { 0, 0, 0, 1, 0 }, new float[] { 0, 0, 0, 0, 1 },
                                        });

                    imgAttributes.SetColorMatrix(gray);

                    using (var g = Graphics.FromImage(grayBitmap)) {
                        g.DrawImage(img, new Rectangle(0, 0, img.Width, img.Height), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, imgAttributes);
                    }
                }
            } 
            catch
            {
                grayBitmap.Dispose();
                throw;
            }
            return grayBitmap;
        }

        public static Image GetRank(int rankIndex)
        {
            return rankImages[Math.Min(rankIndex, rankImages.Length - 1)];
        }

        public static Bitmap GetResized(this Image original, int newWidth, int newHeight, InterpolationMode mode)
        {
            var resized = new Bitmap(newWidth, newHeight);
            using (var g = Graphics.FromImage(resized))
            {
                g.InterpolationMode = mode;
                g.DrawImage(original, 0, 0, newWidth, newHeight);
            }
            return resized;
        }

        public static Region GetRoundedRegion(int cornerRadius, Rectangle rectangle)
        {
            using (var graphicsPath = new GraphicsPath()) {
                graphicsPath.AddArc(rectangle.X, rectangle.Y, cornerRadius, cornerRadius, 180, 90);
                graphicsPath.AddArc(rectangle.X + rectangle.Width - cornerRadius, rectangle.Y, cornerRadius, cornerRadius, 270, 90);
                graphicsPath.AddArc(rectangle.X + rectangle.Width - cornerRadius,
                                    rectangle.Y + rectangle.Height - cornerRadius,
                                    cornerRadius,
                                    cornerRadius,
                                    0,
                                    90);
                graphicsPath.AddArc(rectangle.X, rectangle.Y + rectangle.Height - cornerRadius, cornerRadius, cornerRadius, 90, 90);
                return new Region(graphicsPath);
            }
        }

        static Bitmap SafeOpen(string resourceUri)
        {
            try
            {
							var stream = assembly.GetManifestResourceStream(resourceUri);
                return stream == null ? null : new Bitmap(stream);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error - cannot get resource {0}: {1}", resourceUri, ex);
                return null;
            }
        }
    }
}