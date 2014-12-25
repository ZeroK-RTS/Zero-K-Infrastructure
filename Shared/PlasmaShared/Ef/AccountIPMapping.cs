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
    // AccountIP
    internal partial class AccountIPMapping : EntityTypeConfiguration<AccountIP>
    {
        public AccountIPMapping(string schema = "dbo")
        {
            ToTable(schema + ".AccountIP");
            HasKey(x => new { x.AccountID, x.IP });

            Property(x => x.AccountID).HasColumnName("AccountID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.IP).HasColumnName("IP").IsRequired().HasMaxLength(50).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.LoginCount).HasColumnName("LoginCount").IsRequired();
            Property(x => x.FirstLogin).HasColumnName("FirstLogin").IsRequired();
            Property(x => x.LastLogin).HasColumnName("LastLogin").IsRequired();

            // Foreign keys
            HasRequired(a => a.Account).WithMany(b => b.AccountIPS).HasForeignKey(c => c.AccountID); // FK_AccountIP_Account
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
