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
    // KudosPurchase
    internal partial class KudosPurchaseMapping : EntityTypeConfiguration<KudosPurchase>
    {
        public KudosPurchaseMapping(string schema = "dbo")
        {
            ToTable(schema + ".KudosPurchase");
            HasKey(x => x.KudosPurchaseID);

            Property(x => x.KudosPurchaseID).HasColumnName("KudosPurchaseID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.AccountID).HasColumnName("AccountID").IsRequired();
            Property(x => x.KudosValue).HasColumnName("KudosValue").IsRequired();
            Property(x => x.Time).HasColumnName("Time").IsRequired();
            Property(x => x.UnlockID).HasColumnName("UnlockID").IsOptional();

            // Foreign keys
            HasRequired(a => a.Account).WithMany(b => b.KudosPurchases).HasForeignKey(c => c.AccountID); // FK_KudosChange_Account
            HasOptional(a => a.Unlock).WithMany(b => b.KudosPurchases).HasForeignKey(c => c.UnlockID); // FK_KudosChange_Unlock
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
