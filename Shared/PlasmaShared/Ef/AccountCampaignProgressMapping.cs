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
    // AccountCampaignProgress
    internal partial class AccountCampaignProgressMapping : EntityTypeConfiguration<AccountCampaignProgress>
    {
        public AccountCampaignProgressMapping(string schema = "dbo")
        {
            ToTable(schema + ".AccountCampaignProgress");
            HasKey(x => new { x.AccountID, x.CampaignID, x.PlanetID });

            Property(x => x.AccountID).HasColumnName("AccountID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.CampaignID).HasColumnName("CampaignID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.PlanetID).HasColumnName("PlanetID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.IsUnlocked).HasColumnName("IsUnlocked").IsRequired();
            Property(x => x.IsCompleted).HasColumnName("IsCompleted").IsRequired();

            // Foreign keys
            HasRequired(a => a.Account).WithMany(b => b.AccountCampaignProgresses).HasForeignKey(c => c.AccountID); // FK_AccountCampaignProgress_Account
            HasRequired(a => a.CampaignPlanet).WithMany(b => b.AccountCampaignProgresses).HasForeignKey(c => new { c.CampaignID, c.PlanetID }); // FK_AccountCampaignProgress_CampaignPlanet
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
