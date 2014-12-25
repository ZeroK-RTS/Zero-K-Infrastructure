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
    // SpringBattle
    internal partial class SpringBattleMapping : EntityTypeConfiguration<SpringBattle>
    {
        public SpringBattleMapping(string schema = "dbo")
        {
            ToTable(schema + ".SpringBattle");
            HasKey(x => x.SpringBattleID);

            Property(x => x.SpringBattleID).HasColumnName("SpringBattleID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.EngineGameID).HasColumnName("EngineGameID").IsOptional().IsUnicode(false).HasMaxLength(64);
            Property(x => x.HostAccountID).HasColumnName("HostAccountID").IsRequired();
            Property(x => x.Title).HasColumnName("Title").IsOptional().HasMaxLength(200);
            Property(x => x.MapResourceID).HasColumnName("MapResourceID").IsRequired();
            Property(x => x.ModResourceID).HasColumnName("ModResourceID").IsRequired();
            Property(x => x.StartTime).HasColumnName("StartTime").IsRequired();
            Property(x => x.Duration).HasColumnName("Duration").IsRequired();
            Property(x => x.PlayerCount).HasColumnName("PlayerCount").IsRequired();
            Property(x => x.HasBots).HasColumnName("HasBots").IsRequired();
            Property(x => x.IsMission).HasColumnName("IsMission").IsRequired();
            Property(x => x.ReplayFileName).HasColumnName("ReplayFileName").IsOptional().HasMaxLength(500);
            Property(x => x.EngineVersion).HasColumnName("EngineVersion").IsOptional().HasMaxLength(100);
            Property(x => x.IsEloProcessed).HasColumnName("IsEloProcessed").IsRequired();
            Property(x => x.WinnerTeamXpChange).HasColumnName("WinnerTeamXpChange").IsOptional();
            Property(x => x.LoserTeamXpChange).HasColumnName("LoserTeamXpChange").IsOptional();
            Property(x => x.ForumThreadID).HasColumnName("ForumThreadID").IsOptional();
            Property(x => x.TeamsTitle).HasColumnName("TeamsTitle").IsOptional().HasMaxLength(250);
            Property(x => x.IsFfa).HasColumnName("IsFfa").IsRequired();
            Property(x => x.RatingPollID).HasColumnName("RatingPollID").IsOptional();

            // Foreign keys
            HasRequired(a => a.Account).WithMany(b => b.SpringBattles).HasForeignKey(c => c.HostAccountID); // FK_SpringBattle_Account
            HasOptional(a => a.ForumThread).WithMany(b => b.SpringBattles).HasForeignKey(c => c.ForumThreadID); // FK_SpringBattle_ForumThread
            HasOptional(a => a.RatingPoll).WithMany(b => b.SpringBattles).HasForeignKey(c => c.RatingPollID); // FK_SpringBattle_RatingPoll
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
