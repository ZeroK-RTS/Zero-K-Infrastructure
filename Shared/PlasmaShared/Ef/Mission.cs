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
    // Mission
    public partial class Mission
    {
        public int MissionID { get; set; } // MissionID (Primary key)
        public string Name { get; set; } // Name
        public string Mod { get; set; } // Mod
        public string Map { get; set; } // Map
        public byte[] Mutator { get; set; } // Mutator
        public byte[] Image { get; set; } // Image
        public string Description { get; set; } // Description
        public string DescriptionStory { get; set; } // DescriptionStory
        public DateTime CreatedTime { get; set; } // CreatedTime
        public DateTime ModifiedTime { get; set; } // ModifiedTime
        public string ScoringMethod { get; set; } // ScoringMethod
        public string TopScoreLine { get; set; } // TopScoreLine
        public string MissionEditorVersion { get; set; } // MissionEditorVersion
        public string SpringVersion { get; set; } // SpringVersion
        public int Revision { get; set; } // Revision
        public string Script { get; set; } // Script
        public string TokenCondition { get; set; } // TokenCondition
        public int? CampaignID { get; set; } // CampaignID
        public int AccountID { get; set; } // AccountID
        public string ModOptions { get; set; } // ModOptions
        public string ModRapidTag { get; set; } // ModRapidTag
        public int MinHumans { get; set; } // MinHumans
        public int MaxHumans { get; set; } // MaxHumans
        public bool IsScriptMission { get; set; } // IsScriptMission
        public int MissionRunCount { get; set; } // MissionRunCount
        public bool IsDeleted { get; set; } // IsDeleted
        public string ManualDependencies { get; set; } // ManualDependencies
        public float? Rating { get; set; } // Rating
        public float? Difficulty { get; set; } // Difficulty
        public bool IsCoop { get; set; } // IsCoop
        public int ForumThreadID { get; set; } // ForumThreadID
        public float? FeaturedOrder { get; set; } // FeaturedOrder
        public int? RatingPollID { get; set; } // RatingPollID
        public int? DifficultyRatingPollID { get; set; } // DifficultyRatingPollID

        // Reverse navigation
        public virtual ICollection<CampaignPlanet> CampaignPlanets { get; set; } // CampaignPlanet.FK_CampaignPlanet_Mission
        public virtual ICollection<MissionScore> MissionScores { get; set; } // Many to many mapping
        public virtual ICollection<Rating> Ratings { get; set; } // Rating.FK_Rating_Mission
        public virtual ICollection<Resource> Resources { get; set; } // Resource.FK_Resource_Mission
        public virtual Mission Mission2 { get; set; } // Mission.FK_Mission_Mission

        // Foreign keys
        public virtual Account Account { get; set; } // FK_Mission_Account
        public virtual ForumThread ForumThread { get; set; } // FK_Mission_ForumThread
        public virtual Mission Mission1 { get; set; } // FK_Mission_Mission
        public virtual RatingPoll RatingPoll_DifficultyRatingPollID { get; set; } // FK_Mission_RatingPoll1
        public virtual RatingPoll RatingPoll_RatingPollID { get; set; } // FK_Mission_RatingPoll

        public Mission()
        {
            Revision = 1;
            MinHumans = 1;
            MaxHumans = 1;
            IsScriptMission = false;
            MissionRunCount = 0;
            IsDeleted = false;
            IsCoop = false;
            CampaignPlanets = new List<CampaignPlanet>();
            MissionScores = new List<MissionScore>();
            Ratings = new List<Rating>();
            Resources = new List<Resource>();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
