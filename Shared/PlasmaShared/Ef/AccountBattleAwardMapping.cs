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
    // AccountBattleAward
    internal partial class AccountBattleAwardMapping : EntityTypeConfiguration<AccountBattleAward>
    {
        public AccountBattleAwardMapping(string schema = "dbo")
        {
            ToTable(schema + ".AccountBattleAward");
            HasKey(x => new { x.AccountID, x.SpringBattleID, x.AwardKey });

            Property(x => x.AccountID).HasColumnName("AccountID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.SpringBattleID).HasColumnName("SpringBattleID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.AwardKey).HasColumnName("AwardKey").IsRequired().IsUnicode(false).HasMaxLength(50).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.AwardDescription).HasColumnName("AwardDescription").IsOptional().HasMaxLength(500);
            Property(x => x.Value).HasColumnName("Value").IsOptional();

            // Foreign keys
            HasRequired(a => a.Account).WithMany(b => b.AccountBattleAwards).HasForeignKey(c => c.AccountID); // FK_AccountBattleAward_Account
            HasRequired(a => a.SpringBattle).WithMany(b => b.AccountBattleAwards).HasForeignKey(c => c.SpringBattleID); // FK_AccountBattleAward_SpringBattle
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
