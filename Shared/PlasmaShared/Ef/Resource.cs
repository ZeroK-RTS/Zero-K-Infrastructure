using System.Security.Cryptography.X509Certificates;
// ReSharper disable RedundantUsingDirective
// ReSharper disable DoNotCallOverridableMethodsInConstructor
// ReSharper disable InconsistentNaming
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable PartialMethodWithSinglePart
// ReSharper disable RedundantNameQualifier
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
//using DatabaseGeneratedOption = System.ComponentModel.DataAnnotations.DatabaseGeneratedOption;

namespace ZkData
{
    // Resource
    public partial class Resource
    {
        public int ResourceID { get; set; } // ResourceID (Primary key)
        public string InternalName { get; set; } // InternalName
        public ResourceType TypeID { get; set; } // TypeID
        public DateTime? LastLinkCheck { get; set; } // LastLinkCheck
        public int DownloadCount { get; set; } // DownloadCount
        public int NoLinkDownloadCount { get; set; } // NoLinkDownloadCount
        public int? MissionID { get; set; } // MissionID
        public DateTime? LastChange { get; set; } // LastChange
        public string AuthorName { get; set; } // AuthorName
        public string MapTags { get; set; } // MapTags
        public int? MapWidth { get; set; } // MapWidth
        public int? MapHeight { get; set; } // MapHeight
        public int? MapSizeSquared { get; set; } // MapSizeSquared
        public float? MapSizeRatio { get; set; } // MapSizeRatio
        public bool? MapIsSupported { get; set; } // MapIsSupported
        public bool? MapIsAssymetrical { get; set; } // MapIsAssymetrical
        public int? MapHills { get; set; } // MapHills
        public int? MapWaterLevel { get; set; } // MapWaterLevel
        public bool? MapIs1v1 { get; set; } // MapIs1v1
        public bool? MapIsTeams { get; set; } // MapIsTeams
        public bool? MapIsFfa { get; set; } // MapIsFfa
        public bool? MapIsChickens { get; set; } // MapIsChickens
        public bool? MapIsSpecial { get; set; } // MapIsSpecial
        public int? MapFFAMaxTeams { get; set; } // MapFFAMaxTeams
        public int? MapRatingCount { get; set; } // MapRatingCount
        public int? MapRatingSum { get; set; } // MapRatingSum
        public int? TaggedByAccountID { get; set; } // TaggedByAccountID
        public int? ForumThreadID { get; set; } // ForumThreadID
        public float? FeaturedOrder { get; set; } // FeaturedOrder
        public string MapPlanetWarsIcon { get; set; } // MapPlanetWarsIcon
        public int? RatingPollID { get; set; } // RatingPollID
        public string MapSpringieCommands { get; set; } // MapSpringieCommands

        // Reverse navigation
        public virtual ICollection<MapRating> MapRatings { get; set; } // Many to many mapping
        public virtual ICollection<Planet> Planets { get; set; } // Planet.FK_Planet_Resource
        public virtual ICollection<ResourceContentFile> ResourceContentFiles { get; set; } // Many to many mapping
        public virtual ICollection<ResourceDependency> ResourceDependencies { get; set; } // Many to many mapping
        public virtual ICollection<ResourceSpringHash> ResourceSpringHashes { get; set; } // Many to many mapping

        [InverseProperty("ResourceByMapResourceID")]
        public virtual ICollection<SpringBattle> SpringBattlesByMapResourceID { get; set; } // SpringBattle.FK_SpringBattle_Resource1

        [InverseProperty("ResourceByModResourceID")]
        public virtual ICollection<SpringBattle> SpringBattlesByModResourceID { get; set; } // SpringBattle.FK_SpringBattle_Resource1


        // Foreign keys
        public virtual Account Account { get; set; } // FK_Resource_Account
        public virtual ForumThread ForumThread { get; set; } // FK_Resource_ForumThread
        public virtual Mission Mission { get; set; } // FK_Resource_Mission
        public virtual RatingPoll RatingPoll { get; set; } // FK_Resource_RatingPoll

        public Resource()
        {
            MapRatings = new List<MapRating>();
            Planets = new List<Planet>();
            ResourceContentFiles = new List<ResourceContentFile>();
            ResourceDependencies = new List<ResourceDependency>();
            ResourceSpringHashes = new List<ResourceSpringHash>();
            SpringBattlesByMapResourceID = new List<SpringBattle>();
            SpringBattlesByModResourceID = new List<SpringBattle>();
            LastChange = DateTime.UtcNow;
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
