using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace ZkData
{
    [DataContract]
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
        [DataMember]
        public string Name { get; set; }

        [StringLength(100)]
        [DataMember]
        public string Mod { get; set; }

        [StringLength(100)]
        [DataMember]
        public string Map { get; set; }

        [DataMember]
        public virtual byte[] Mutator { get; set; }

        [Required]
        [DataMember]
        public byte[] Image { get; set; }

        [Column(TypeName = "text")]
        [DataMember]
        public string Description { get; set; }

        [Column(TypeName = "text")]
        [DataMember]
        public string DescriptionStory { get; set; }

        [DataMember]
        public DateTime CreatedTime { get; set; }

        [DataMember]
        public DateTime ModifiedTime { get; set; }

        [StringLength(500)]
        [DataMember]
        public string ScoringMethod { get; set; }

        [StringLength(100)]
        [DataMember]
        public string TopScoreLine { get; set; }

        [StringLength(20)]
        [DataMember]
        public string MissionEditorVersion { get; set; }

        [StringLength(100)]
        [DataMember]
        public string SpringVersion { get; set; }

        [DataMember]
        public int Revision { get; set; }

        [Required]
        [DataMember]
        public string Script { get; set; }

        [StringLength(500)]
        [DataMember]
        public string TokenCondition { get; set; }

        [DataMember]
        public int? CampaignID { get; set; }

        [DataMember]
        public int AccountID { get; set; }

        [StringLength(4000)]
        [DataMember]
        public string ModOptions { get; set; }

        [StringLength(100)]
        [DataMember]
        public string ModRapidTag { get; set; }

        [DataMember]
        public int MinHumans { get; set; }

        [DataMember]
        public int MaxHumans { get; set; }

        [DataMember]
        public bool IsScriptMission { get; set; }

        [DataMember]
        public int MissionRunCount { get; set; }

        [DataMember]
        public bool IsDeleted { get; set; }

        [StringLength(1000)]
        [DataMember]
        public string ManualDependencies { get; set; }

        [DataMember]
        public float? Rating { get; set; }

        [DataMember]
        public float? Difficulty { get; set; }

        [DataMember]
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
