using System;
using System.Globalization;

namespace ZeroKWeb
{
    /// <summary>
    /// Geometry for the PlanetWars galaxy map links.
    ///
    /// A link between two planets is drawn as a thin rectangle rather than a line, because
    /// the original Raphael implementation needed a gradient and Raphael's lines do not
    /// support them. Native SVG has the same restriction, so the rectangle stays: it is
    /// laid out pointing straight down from the first planet and then rotated onto the
    /// second.
    ///
    /// This lives in its own file, free of System.Web, so it can be linked into the .NET 9
    /// test project - see Tests.Portable/Tests.Portable.csproj.
    /// </summary>
    public static class GalaxyMapGeometry
    {
        /// <summary>A link rectangle, before rotation, plus the rotation to apply.</summary>
        public struct LinkRect
        {
            public double X;
            public double Y;
            public double Width;
            public double Length;

            /// <summary>Degrees, clockwise, about the first planet's centre.</summary>
            public double AngleDegrees;
        }

        /// <summary>
        /// Lays out the rectangle for a link running from (x1,y1) to (x2,y2).
        /// <paramref name="startOffset"/> pushes the near end clear of the first planet's
        /// icon, <paramref name="endTrim"/> shortens the far end clear of the second's.
        /// </summary>
        public static LinkRect Link(double x1, double y1, double x2, double y2, double width, double startOffset = 0, double endTrim = 0)
        {
            var dx = x2 - x1;
            var dy = y2 - y1;
            var distance = Math.Sqrt(dx * dx + dy * dy);

            return new LinkRect
            {
                X = x1 - width / 2,
                Y = y1 + startOffset,
                Width = width,
                // A link shorter than the icons it has to clear would give a negative
                // height, which is an error in SVG. Raphael silently drew nothing useful
                // for those; clamping keeps the document valid.
                Length = Math.Max(0, distance - endTrim),
                AngleDegrees = -Math.Atan2(dx, dy) / Math.PI * 180,
            };
        }

        /// <summary>
        /// Formats a number for an SVG attribute. Always invariant: the server's culture
        /// must not decide whether a coordinate reads "350.5" or "350,5".
        /// </summary>
        public static string N(double value)
        {
            return Math.Round(value, 2).ToString(CultureInfo.InvariantCulture);
        }
    }
}
