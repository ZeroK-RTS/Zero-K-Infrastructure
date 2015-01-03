using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;

namespace ZkData
{
    partial class Resource
    {
        [NotMapped]
        public double MapDiagonal
        {
            get { return Math.Sqrt((MapWidth * MapWidth + MapHeight * MapHeight) ?? 0); }
        }

        public enum WaterLevel
        {
            Land = 1,
            Mixed = 2,
            Sea = 3
        }

        public enum Hill
        {
            Flat = 1,
            Hills = 2,
            Mountains = 3
        }

        [NotMapped]
        public double? MapRating
        {
            get
            {
                if (MapRatingCount > 0) return MapRatingSum / MapRatingCount;
                else return null;
            }
        }


        [NotMapped]
        public int PlanetWarsIconSize
        {
            get { return (int)(25 + MapDiagonal); }
        }

        public Size ScaledImageSize(int maxSize)
        {
            var s = new Size();
            if (MapSizeRatio > 1)
            {
                s.Width = maxSize;
                s.Height = (int)(maxSize / MapSizeRatio);
            }
            else if (MapSizeRatio < 1)
            {
                s.Height = maxSize;
                s.Width = (int)(maxSize * MapSizeRatio);
            }
            else
            {
                s.Width = maxSize;
                s.Height = maxSize;
            }
            return s;
        }

        [NotMapped]
        public string ThumbnailName
        {
            get { return string.Concat((string)InternalName.EscapePath(), ".thumbnail.jpg"); }
        }

        [NotMapped]
        public string MinimapName
        {
            get { return string.Concat((string)InternalName.EscapePath(), ".minimap.jpg"); }
        }

        [NotMapped]
        public string MetadataName
        {
            get { return string.Concat((string)InternalName.EscapePath(), ".metadata.xml.gz"); }
        }

        [NotMapped]
        public string HeightmapName
        {
            get { return string.Concat((string)InternalName.EscapePath(), ".heightmap.jpg"); }
        }

        [NotMapped]
        public string MetalmapName
        {
            get { return string.Concat((string)InternalName.EscapePath(), ".metalmap.jpg"); }
        }


    }
}
