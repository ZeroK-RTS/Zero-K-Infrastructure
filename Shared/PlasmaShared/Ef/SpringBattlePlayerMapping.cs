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
    // SpringBattlePlayer
    internal partial class SpringBattlePlayerMapping : EntityTypeConfiguration<SpringBattlePlayer>
    {
        public SpringBattlePlayerMapping(string schema = "dbo")
        {
            ToTable(schema + ".SpringBattlePlayer");
            HasKey(x => new { x.SpringBattleID, x.AccountID });

            Property(x => x.SpringBattleID).HasColumnName("SpringBattleID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.AccountID).HasColumnName("AccountID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.IsSpectator).HasColumnName("IsSpectator").IsRequired();
            Property(x => x.IsInVictoryTeam).HasColumnName("IsInVictoryTeam").IsRequired();
            Property(x => x.CommanderType).HasColumnName("CommanderType").IsOptional().HasMaxLength(50);
            Property(x => x.LoseTime).HasColumnName("LoseTime").IsOptional();
            Property(x => x.AllyNumber).HasColumnName("AllyNumber").IsRequired();
            Property(x => x.Rank).HasColumnName("Rank").IsRequired();
            Property(x => x.EloChange).HasColumnName("EloChange").IsOptional();
            Property(x => x.XpChange).HasColumnName("XpChange").IsOptional();
            Property(x => x.Influence).HasColumnName("Influence").IsOptional();

            // Foreign keys
            HasRequired(a => a.SpringBattle).WithMany(b => b.SpringBattlePlayers).HasForeignKey(c => c.SpringBattleID); // FK_SpringBattlePlayer_SpringBattle
            HasRequired(a => a.Account).WithMany(b => b.SpringBattlePlayers).HasForeignKey(c => c.AccountID); // FK_SpringBattlePlayer_Account
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
