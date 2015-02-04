using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;

namespace ZkData
{
    public class Resource
    {
        
        public Resource()
        {
            MapRatings = new HashSet<MapRating>();
            Planets = new HashSet<Planet>();
            ResourceContentFiles = new HashSet<ResourceContentFile>();
            ResourceDependencies = new HashSet<ResourceDependency>();
            SpringBattlesByMapResourceID = new HashSet<SpringBattle>();
            SpringBattlesByModID = new HashSet<SpringBattle>();
        }

        public int ResourceID { get; set; }
        [Required]
        [StringLength(255)]
        public string InternalName { get; set; }
        public ResourceType TypeID { get; set; }
        public DateTime? LastLinkCheck { get; set; }
        public int DownloadCount { get; set; }
        public int NoLinkDownloadCount { get; set; }
        public int? MissionID { get; set; }
        public DateTime? LastChange { get; set; }
        [StringLength(200)]
        public string AuthorName { get; set; }
        public string MapTags { get; set; }
        public int? MapWidth { get; set; }
        public int? MapHeight { get; set; }
        public int? MapSizeSquared { get; set; }
        public float? MapSizeRatio { get; set; }
        public bool? MapIsSupported { get; set; }
        public bool? MapIsAssymetrical { get; set; }
        public int? MapHills { get; set; }
        public int? MapWaterLevel { get; set; }
        public bool? MapIs1v1 { get; set; }
        public bool? MapIsTeams { get; set; }
        public bool? MapIsFfa { get; set; }
        public bool? MapIsChickens { get; set; }
        public bool? MapIsSpecial { get; set; }
        public int? MapFFAMaxTeams { get; set; }
        public int? MapRatingCount { get; set; }
        public int? MapRatingSum { get; set; }
        public int? TaggedByAccountID { get; set; }
        public int? ForumThreadID { get; set; }
        public float? FeaturedOrder { get; set; }
        [StringLength(50)]
        public string MapPlanetWarsIcon { get; set; }
        public int? RatingPollID { get; set; }
        [StringLength(2000)]
        public string MapSpringieCommands { get; set; }
        
        public virtual Account Account { get; set; }
        public virtual ForumThread ForumThread { get; set; }
        public virtual ICollection<MapRating> MapRatings { get; set; }
        public virtual Mission Mission { get; set; }
        public virtual ICollection<Planet> Planets { get; set; }
        public virtual ICollection<ResourceContentFile> ResourceContentFiles { get; set; }
        public virtual ICollection<ResourceDependency> ResourceDependencies { get; set; }
        public virtual ICollection<SpringBattle> SpringBattlesByModID { get; set; }
        public virtual ICollection<SpringBattle> SpringBattlesByMapResourceID { get; set; }



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
