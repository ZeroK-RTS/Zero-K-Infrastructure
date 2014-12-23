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
    // AccountCampaignVar
    internal partial class AccountCampaignVarMapping : EntityTypeConfiguration<AccountCampaignVar>
    {
        public AccountCampaignVarMapping(string schema = "dbo")
        {
            ToTable(schema + ".AccountCampaignVar");
            HasKey(x => new { x.AccountID, x.CampaignID, x.VarID });

            Property(x => x.AccountID).HasColumnName("AccountID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.CampaignID).HasColumnName("CampaignID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.VarID).HasColumnName("VarID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.Value).HasColumnName("Value").IsRequired();

            // Foreign keys
            HasRequired(a => a.Account).WithMany(b => b.AccountCampaignVars).HasForeignKey(c => c.AccountID); // FK_AccountCampaignVar_Account
            HasRequired(a => a.CampaignVar).WithMany(b => b.AccountCampaignVars).HasForeignKey(c => new { c.CampaignID, c.VarID }); // FK_AccountCampaignVar_CampaignVar
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
