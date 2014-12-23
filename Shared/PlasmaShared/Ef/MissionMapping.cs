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

namespace PlasmaShared.Ef
{
    // Mission
    internal partial class MissionMapping : EntityTypeConfiguration<Mission>
    {
        public MissionMapping(string schema = "dbo")
        {
            ToTable(schema + ".Mission");
            HasKey(x => x.MissionID);

            Property(x => x.MissionID).HasColumnName("MissionID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.Name).HasColumnName("Name").IsRequired().HasMaxLength(200);
            Property(x => x.Mod).HasColumnName("Mod").IsOptional().HasMaxLength(100);
            Property(x => x.Map).HasColumnName("Map").IsOptional().HasMaxLength(100);
            Property(x => x.Mutator).HasColumnName("Mutator").IsOptional();
            Property(x => x.Image).HasColumnName("Image").IsRequired();
            Property(x => x.Description).HasColumnName("Description").IsOptional().IsUnicode(false).HasMaxLength(2147483647);
            Property(x => x.DescriptionStory).HasColumnName("DescriptionStory").IsOptional().IsUnicode(false).HasMaxLength(2147483647);
            Property(x => x.CreatedTime).HasColumnName("CreatedTime").IsRequired();
            Property(x => x.ModifiedTime).HasColumnName("ModifiedTime").IsRequired();
            Property(x => x.ScoringMethod).HasColumnName("ScoringMethod").IsOptional().HasMaxLength(500);
            Property(x => x.TopScoreLine).HasColumnName("TopScoreLine").IsOptional().HasMaxLength(100);
            Property(x => x.MissionEditorVersion).HasColumnName("MissionEditorVersion").IsOptional().HasMaxLength(20);
            Property(x => x.SpringVersion).HasColumnName("SpringVersion").IsOptional().HasMaxLength(100);
            Property(x => x.Revision).HasColumnName("Revision").IsRequired();
            Property(x => x.Script).HasColumnName("Script").IsRequired();
            Property(x => x.TokenCondition).HasColumnName("TokenCondition").IsOptional().IsUnicode(false).HasMaxLength(500);
            Property(x => x.CampaignID).HasColumnName("CampaignID").IsOptional();
            Property(x => x.AccountID).HasColumnName("AccountID").IsRequired();
            Property(x => x.ModOptions).HasColumnName("ModOptions").IsOptional();
            Property(x => x.ModRapidTag).HasColumnName("ModRapidTag").IsOptional().HasMaxLength(100);
            Property(x => x.MinHumans).HasColumnName("MinHumans").IsRequired();
            Property(x => x.MaxHumans).HasColumnName("MaxHumans").IsRequired();
            Property(x => x.IsScriptMission).HasColumnName("IsScriptMission").IsRequired();
            Property(x => x.MissionRunCount).HasColumnName("MissionRunCount").IsRequired();
            Property(x => x.IsDeleted).HasColumnName("IsDeleted").IsRequired();
            Property(x => x.ManualDependencies).HasColumnName("ManualDependencies").IsOptional();
            Property(x => x.Rating).HasColumnName("Rating").IsOptional();
            Property(x => x.Difficulty).HasColumnName("Difficulty").IsOptional();
            Property(x => x.IsCoop).HasColumnName("IsCoop").IsRequired();
            Property(x => x.ForumThreadID).HasColumnName("ForumThreadID").IsRequired();
            Property(x => x.FeaturedOrder).HasColumnName("FeaturedOrder").IsOptional();
            Property(x => x.RatingPollID).HasColumnName("RatingPollID").IsOptional();
            Property(x => x.DifficultyRatingPollID).HasColumnName("DifficultyRatingPollID").IsOptional();

            // Foreign keys
            HasRequired(a => a.Mission1).WithOptional(b => b.Mission2); // FK_Mission_Mission
            HasRequired(a => a.Account).WithMany(b => b.Missions).HasForeignKey(c => c.AccountID); // FK_Mission_Account
            HasRequired(a => a.ForumThread).WithMany(b => b.Missions).HasForeignKey(c => c.ForumThreadID); // FK_Mission_ForumThread
            HasOptional(a => a.RatingPoll_RatingPollID).WithMany(b => b.Missions_RatingPollID).HasForeignKey(c => c.RatingPollID); // FK_Mission_RatingPoll
            HasOptional(a => a.RatingPoll_DifficultyRatingPollID).WithMany(b => b.Missions_DifficultyRatingPollID).HasForeignKey(c => c.DifficultyRatingPollID); // FK_Mission_RatingPoll1
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
