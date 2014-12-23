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
    // AutoBanSmurfList
    internal partial class AutoBanSmurfListMapping : EntityTypeConfiguration<AutoBanSmurfList>
    {
        public AutoBanSmurfListMapping(string schema = "dbo")
        {
            ToTable(schema + ".AutoBanSmurfList");
            HasKey(x => x.AccountID);

            Property(x => x.AccountID).HasColumnName("AccountID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.BanLobby).HasColumnName("BanLobby").IsRequired();
            Property(x => x.BanSite).HasColumnName("BanSite").IsRequired();
            Property(x => x.BanIP).HasColumnName("BanIP").IsRequired();
            Property(x => x.BanUserID).HasColumnName("BanUserID").IsRequired();

            // Foreign keys
            HasRequired(a => a.Account).WithOptional(b => b.AutoBanSmurfList); // FK_AutoBanSmurfList_Account
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
