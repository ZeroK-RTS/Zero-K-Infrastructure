using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using PlanetWarsShared;

namespace PlanetWars
{
    public class LinkImageGenerator
    {
        static readonly Color neutralColor = Color.FromArgb(0, 1, 1, 1);

        public LinkImageGenerator(Size mapSize, Galaxy galaxy, string path) : this(mapSize, galaxy, path, ImageFormat.Png) {}

        public LinkImageGenerator(Size mapSize, Galaxy galaxy, string path, ImageFormat imageFormat)
        {
            MapSize = mapSize;
            Galaxy = galaxy;
            ImagePath = path;
            Directory.CreateDirectory(ImagePath);
            ImageFormat = imageFormat;
            Padding = 10;
        }

        public const int AntiAliasFactor = 1;
        public const float ArrowOffset  = 25;
        public int Padding { get; set; }
        public const float LineWidth = 2;

        public string ImagePath { get; set; }
        public ImageFormat ImageFormat { get; set; }

        public const bool DrawLinksBetweenNeutralPlanets = false;

        public Size MapSize { get; private set; }
        public Galaxy Galaxy { get; private set; }



        public void GenerateImages()
        {
            var factionNames = Galaxy.Factions.Select(f => f.Name).ToArray();
            var possibleOwners = factionNames.ToList();
            possibleOwners.Add(null);
            var links = from link in Galaxy.Links
                        from faction1 in possibleOwners
                        from faction2 in possibleOwners
                        from offensiveFaction in factionNames
                        select new {link, faction1, faction2, offensiveFaction};
            links.ForEach(l => GenerateImage(l.link, l.faction1, l.faction2, l.offensiveFaction));
        }

        public void GenerateImage(Link link, string faction1, string faction2, string offensiveFaction)
        {
            var fileName = link.GetFileName(Galaxy, faction1, faction2, offensiveFaction);
            var linkDrawing = GetLinkDrawing(link, faction1, faction2, offensiveFaction);
            if (linkDrawing != null && !File.Exists(fileName)) {
                var bitmapSize = GetImageBounds(link).Size.Multiply(AntiAliasFactor);
                using (var bitmap = new Bitmap(bitmapSize.Width, bitmapSize.Height)) {
                    var location = new Point(Padding*AntiAliasFactor, Padding*AntiAliasFactor);
                    using (var g = Graphics.FromImage(bitmap)) {
                        SetHighQuality(g);
                        linkDrawing.Draw(g, location, MapSize.Multiply(AntiAliasFactor));
                    }
                    bitmap.HighQualityResize(1/(float)AntiAliasFactor).Save(
                        Path.Combine(ImagePath, fileName), ImageFormat);
                }
            }
        }

        static void SetHighQuality(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.CompositingMode = CompositingMode.SourceOver;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        }

        public Rectangle GetImageBounds(Link link)
        {
            var planetPositions = Galaxy.GetPlanets(link).Select(p => p.Position).ToArray();
            planetPositions = planetPositions.Select(p => p.Scale(MapSize)).ToArray(); // to image coords
            return planetPositions.ToRectangleF().ToRectangle().PadRectangle(Padding);
        }

        public LinkDrawing GetLinkDrawing(Link link, string faction1, string faction2, string offensiveFaction)
        {
            var planets = link.PlanetIDs.Select(id => Galaxy.GetPlanet(id)).ToArray();
            var factionNames = new[] { faction1, faction2 };

            if (!DrawLinksBetweenNeutralPlanets && factionNames.All(f => f == null)) {
                return null;
            }

            var isColonyLink = factionNames.Contains(null);

            var isArrowNeeded = factionNames[0] != factionNames[1] &&
                                (isColonyLink || factionNames.Contains(offensiveFaction));

            var points = planets.Select(p => p.Position).ToArray();
            var colors = factionNames.Select(f => f == null ? neutralColor : Galaxy.GetFaction(f).Color).ToArray();

            var linkDrawing = new LinkDrawing();

            if (isArrowNeeded) {
                var arrowTipIndex = Array.FindIndex(factionNames, f => isColonyLink ? f == null : f != offensiveFaction);
                var arrowEndIndex = arrowTipIndex == 0 ? 1 : 0;

                linkDrawing.IsArrow = true;
                linkDrawing.IsColonyLink = isColonyLink;

                linkDrawing.Tip = points[arrowTipIndex];
                linkDrawing.End = points[arrowEndIndex];
                linkDrawing.TipColor = colors[arrowTipIndex];
                linkDrawing.EndColor = colors[arrowEndIndex];

                // shorten the arrow so the tip isn't covered by a planet
                linkDrawing.ArrowOffset = ArrowOffset * AntiAliasFactor;
            } else {
                // doesn't matter which is the tip
                linkDrawing.Tip = points[0];
                linkDrawing.End = points[1];
                linkDrawing.TipColor = colors[0];
                linkDrawing.EndColor = colors[1];
            }

            linkDrawing.Location = PointF.Empty;
            var galaxySize = Program.MainForm.MapBox.Image.Width;
            linkDrawing.LineWidth = LineWidth / galaxySize;

            return linkDrawing;
        }
    }
}