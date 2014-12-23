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
    // AccountUnlock
    internal partial class AccountUnlockMapping : EntityTypeConfiguration<AccountUnlock>
    {
        public AccountUnlockMapping(string schema = "dbo")
        {
            ToTable(schema + ".AccountUnlock");
            HasKey(x => new { x.AccountID, x.UnlockID });

            Property(x => x.AccountID).HasColumnName("AccountID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.UnlockID).HasColumnName("UnlockID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.Count).HasColumnName("Count").IsRequired();

            // Foreign keys
            HasRequired(a => a.Account).WithMany(b => b.AccountUnlocks).HasForeignKey(c => c.AccountID); // FK_AccountUnlock_Account
            HasRequired(a => a.Unlock).WithMany(b => b.AccountUnlocks).HasForeignKey(c => c.UnlockID); // FK_AccountUnlock_Unlock
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
