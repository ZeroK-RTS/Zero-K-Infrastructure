namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Mission")]
    public partial class Mission
    {
        
        public Mission()
        {
            CampaignPlanets = new HashSet<CampaignPlanet>();
            MissionScores = new HashSet<MissionScore>();
            Ratings = new HashSet<Rating>();
            Resources = new HashSet<Resource>();
        }

        public int MissionID { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [StringLength(100)]
        public string Mod { get; set; }

        [StringLength(100)]
        public string Map { get; set; }

        
        public virtual byte[] Mutator { get; set; }

        [Required]
        public byte[] Image { get; set; }

        [Column(TypeName = "text")]
        public string Description { get; set; }

        [Column(TypeName = "text")]
        public string DescriptionStory { get; set; }

        public DateTime CreatedTime { get; set; }

        public DateTime ModifiedTime { get; set; }

        [StringLength(500)]
        public string ScoringMethod { get; set; }

        [StringLength(100)]
        public string TopScoreLine { get; set; }

        [StringLength(20)]
        public string MissionEditorVersion { get; set; }

        [StringLength(100)]
        public string SpringVersion { get; set; }

        public int Revision { get; set; }

        [Required]
        public string Script { get; set; }

        [StringLength(500)]
        public string TokenCondition { get; set; }

        public int? CampaignID { get; set; }

        public int AccountID { get; set; }

        [StringLength(4000)]
        public string ModOptions { get; set; }

        [StringLength(100)]
        public string ModRapidTag { get; set; }

        public int MinHumans { get; set; }

        public int MaxHumans { get; set; }

        public bool IsScriptMission { get; set; }

        public int MissionRunCount { get; set; }

        public bool IsDeleted { get; set; }

        [StringLength(1000)]
        public string ManualDependencies { get; set; }

        public float? Rating { get; set; }

        public float? Difficulty { get; set; }

        public bool IsCoop { get; set; }

        public int ForumThreadID { get; set; }

        public float? FeaturedOrder { get; set; }

        public int? RatingPollID { get; set; }

        public int? DifficultyRatingPollID { get; set; }

        public virtual Account Account { get; set; }

        
        public virtual ICollection<CampaignPlanet> CampaignPlanets { get; set; }

        public virtual ForumThread ForumThread { get; set; }

        public virtual ICollection<MissionScore> MissionScores { get; set; }
        
        public virtual ICollection<Rating> Ratings { get; set; }

       
        public virtual ICollection<Resource> Resources { get; set; }
    }
}
