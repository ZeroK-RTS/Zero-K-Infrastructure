using System;
using System.Drawing;

namespace PlasmaShared.Imaging
{
    /// <summary>
    /// Target sizes for image scaling, separated from the drawing that applies them.
    ///
    /// This is deliberately free of System.Drawing.Common. <see cref="Size"/> comes from
    /// System.Drawing.Primitives, which is part of the .NET 9 shared framework - it is the
    /// imaging types (Bitmap, Graphics, Image) that are Windows-only. So this file is
    /// linked into Tests.Portable and its arithmetic is checked there, while the pixel work
    /// stays where it is. See Shared/PlasmaShared/IMAGING-MIGRATION.md.
    ///
    /// Getting a target size wrong distorts an image without failing, which is why this
    /// half is worth pinning down first.
    /// </summary>
    public static class ImageSizing
    {
        /// <summary>
        /// Largest size with the given width-to-height ratio that fits in a
        /// <paramref name="maxSize"/> square. A ratio above 1 is landscape.
        ///
        /// The ratio is nullable because Resource.MapSizeRatio is: a map whose dimensions
        /// are unknown gets a square, which is what the original comparison chain did by
        /// accident, since both "null > 1" and "null < 1" are false.
        /// </summary>
        public static Size ScaledToFit(double? widthToHeightRatio, int maxSize)
        {
            if (widthToHeightRatio > 1) return new Size(maxSize, (int)(maxSize / widthToHeightRatio.Value));
            if (widthToHeightRatio < 1) return new Size((int)(maxSize * widthToHeightRatio.Value), maxSize);
            return new Size(maxSize, maxSize);
        }

        /// <summary>
        /// Height that keeps <paramref name="width"/> x <paramref name="height"/> in
        /// proportion when the width is set to <paramref name="targetWidth"/>.
        /// </summary>
        public static int ProportionalHeight(int width, int height, int targetWidth)
        {
            if (width <= 0) return 0;
            return (int)Math.Round((double)targetWidth / width * height);
        }

        /// <summary>
        /// The size <see cref="Utils.ToBytes"/> has always resized to.
        ///
        /// It is preserved here rather than corrected, because changing it changes the
        /// minimaps, metal maps and height maps that AutoRegistrator uploads for every new
        /// map. Note what it does: it takes an image that is ALREADY in proportion and
        /// applies that proportion a second time, so a 2:1 image comes back 4:1. For a
        /// square image it is a no-op, which is why it has survived.
        ///
        /// See ToBytesShouldBe for what it was presumably meant to do.
        /// </summary>
        public static Size LegacyToBytesSize(int width, int height)
        {
            if (height == 0) return new Size(width, 0);
            var ratio = (float)width / height;
            return ratio > 1
                ? new Size(width, (int)(height / ratio))
                : new Size((int)(width * ratio), height);
        }

        /// <summary>
        /// What <see cref="LegacyToBytesSize"/> was presumably meant to do: leave the
        /// proportions alone and bound the longest side by <paramref name="maxSize"/>.
        /// Not wired up - switching to it changes what gets uploaded, which is a decision
        /// rather than a refactor.
        /// </summary>
        public static Size BoundedByLongestSide(int width, int height, int maxSize)
        {
            if (width <= 0 || height <= 0) return new Size(0, 0);
            if (width <= maxSize && height <= maxSize) return new Size(width, height);

            return width >= height
                ? new Size(maxSize, Math.Max(1, (int)Math.Round((double)height * maxSize / width)))
                : new Size(Math.Max(1, (int)Math.Round((double)width * maxSize / height)), maxSize);
        }
    
        /// <summary>
        /// Where a planet's icon goes on the galaxy map, and how big.
        ///
        /// The planet's X and Y are fractions of the canvas, the icon keeps its own aspect ratio,
        /// and the result is CENTRED on that point - which is the part worth having in one tested
        /// place, because getting it wrong shifts every planet by half an icon and still looks
        /// plausible.
        ///
        /// Integer truncation is preserved exactly as the System.Drawing call site had it: the
        /// width truncates before the height is derived from it, so height follows the truncated
        /// width, and the halving truncates again. Computing in doubles and rounding once would be
        /// defensible and would move icons by a pixel, which is not what a port is for.
        /// </summary>
        public static Rectangle PlanetIconPlacement(double planetX, double planetY, Size canvas, Size icon, double iconSize, double zoom)
        {
            var aspect = icon.Height / (double)icon.Width;
            var width = (int)(iconSize * zoom);
            var height = (int)(width * aspect);

            return new Rectangle(
                (int)(planetX * canvas.Width) - width / 2,
                (int)(planetY * canvas.Height) - height / 2,
                width,
                height);
        }

        /// <summary>
        /// unitsync renders every minimap as a SQUARE, whatever shape the map is.
        /// UnitSync.FixAspectRatio stretched it back to the map's real proportions, and this is
        /// that arithmetic.
        ///
        /// Note it is the correct, single application of the ratio - which is what makes
        /// <see cref="LegacyToBytesSize"/> a defect rather than a second opinion: that runs
        /// afterwards, on this already-corrected image, and applies the proportion again.
        /// </summary>
        public static Size MinimapAspectCorrection(Size squareMinimap, Size mapSize)
        {
            if (mapSize.Height == 0) return squareMinimap;
            var ratio = (float)mapSize.Width / mapSize.Height;

            return mapSize.Width > mapSize.Height
                ? new Size(squareMinimap.Width, (int)(squareMinimap.Height / ratio))
                : new Size((int)(squareMinimap.Width * ratio), squareMinimap.Height);
        }

        /// <summary>
        /// Whether a stored map image was written by the old <see cref="LegacyToBytesSize"/> rule.
        ///
        /// **This is what made fixing the rule affordable.** The objection to fixing it was that
        /// old and new images would sit side by side with no way to tell them apart - which turns
        /// out to be false. unitsync's minimap is square; FixAspectRatio stretches it to the map's
        /// true ratio R; the old ToBytes then applied R a second time, leaving R squared. The
        /// database knows R, so an image whose aspect is nearer R squared than R is a legacy one.
        ///
        /// Square maps are excluded because R and R squared are both 1 there - the defect was a
        /// no-op for them, which is exactly why it survived this long.
        /// </summary>
        public static bool LooksLikeLegacyToBytes(Size stored, double? mapSizeRatio)
        {
            if (mapSizeRatio == null || stored.Height == 0 || stored.Width == 0) return false;

            var ratio = mapSizeRatio.Value;
            if (Math.Abs(ratio - 1) < 0.01) return false;

            var actual = (double)stored.Width / stored.Height;
            return Math.Abs(actual - ratio * ratio) < Math.Abs(actual - ratio);
        }

        /// <summary>
        /// The size a legacy stored image should be stretched back to.
        ///
        /// The old rule squashed the SHORT axis, leaving aspect R squared where it should be R, so
        /// the long axis still holds whatever resolution was uploaded and only the short one has
        /// to be restored. Keeping the long axis is deliberate: the detail squashed out cannot be
        /// recovered by any resize, and throwing away the axis that survived would lose more.
        ///
        /// Only <see cref="LooksLikeLegacyToBytes"/> should decide whether to call this - applying
        /// it to an already-correct image would stretch it a second time.
        /// </summary>
        public static Size CorrectedFromLegacy(Size stored, double mapSizeRatio)
        {
            if (stored.Width <= 0 || stored.Height <= 0 || mapSizeRatio <= 0) return stored;

            return ScaledToFit(mapSizeRatio, mapSizeRatio > 1 ? stored.Width : stored.Height);
        }
}
}
