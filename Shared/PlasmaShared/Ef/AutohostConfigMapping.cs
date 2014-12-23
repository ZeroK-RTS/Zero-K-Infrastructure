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
    // AutohostConfig
    internal partial class AutohostConfigMapping : EntityTypeConfiguration<AutohostConfig>
    {
        public AutohostConfigMapping(string schema = "dbo")
        {
            ToTable(schema + ".AutohostConfig");
            HasKey(x => x.AutohostConfigID);

            Property(x => x.AutohostConfigID).HasColumnName("AutohostConfigID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.ClusterNode).HasColumnName("ClusterNode").IsRequired().HasMaxLength(50);
            Property(x => x.Login).HasColumnName("Login").IsRequired().HasMaxLength(50);
            Property(x => x.Password).HasColumnName("Password").IsRequired().HasMaxLength(50);
            Property(x => x.MaxPlayers).HasColumnName("MaxPlayers").IsRequired();
            Property(x => x.Welcome).HasColumnName("Welcome").IsOptional().HasMaxLength(200);
            Property(x => x.AutoSpawn).HasColumnName("AutoSpawn").IsRequired();
            Property(x => x.AutoUpdateRapidTag).HasColumnName("AutoUpdateRapidTag").IsOptional().HasMaxLength(50);
            Property(x => x.SpringVersion).HasColumnName("SpringVersion").IsOptional().HasMaxLength(50);
            Property(x => x.AutoUpdateSpringBranch).HasColumnName("AutoUpdateSpringBranch").IsOptional().HasMaxLength(50);
            Property(x => x.CommandLevels).HasColumnName("CommandLevels").IsOptional().HasMaxLength(500);
            Property(x => x.Map).HasColumnName("Map").IsOptional().HasMaxLength(100);
            Property(x => x.Mod).HasColumnName("Mod").IsOptional().HasMaxLength(100);
            Property(x => x.Title).HasColumnName("Title").IsOptional().HasMaxLength(100);
            Property(x => x.JoinChannels).HasColumnName("JoinChannels").IsOptional().HasMaxLength(100);
            Property(x => x.BattlePassword).HasColumnName("BattlePassword").IsOptional().HasMaxLength(50);
            Property(x => x.AutohostMode).HasColumnName("AutohostMode").IsRequired();
            Property(x => x.MinToStart).HasColumnName("MinToStart").IsOptional();
            Property(x => x.MaxToStart).HasColumnName("MaxToStart").IsOptional();
            Property(x => x.MinToJuggle).HasColumnName("MinToJuggle").IsOptional();
            Property(x => x.MaxToJuggle).HasColumnName("MaxToJuggle").IsOptional();
            Property(x => x.SplitBiggerThan).HasColumnName("SplitBiggerThan").IsOptional();
            Property(x => x.MergeSmallerThan).HasColumnName("MergeSmallerThan").IsOptional();
            Property(x => x.MaxEloDifference).HasColumnName("MaxEloDifference").IsOptional();
            Property(x => x.DontMoveManuallyJoined).HasColumnName("DontMoveManuallyJoined").IsOptional();
            Property(x => x.MinLevel).HasColumnName("MinLevel").IsOptional();
            Property(x => x.MinElo).HasColumnName("MinElo").IsOptional();
            Property(x => x.MaxLevel).HasColumnName("MaxLevel").IsOptional();
            Property(x => x.MaxElo).HasColumnName("MaxElo").IsOptional();
            Property(x => x.IsTrollHost).HasColumnName("IsTrollHost").IsOptional();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
