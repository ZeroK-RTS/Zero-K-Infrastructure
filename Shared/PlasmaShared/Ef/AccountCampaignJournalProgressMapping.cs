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
    // AccountCampaignJournalProgress
    internal partial class AccountCampaignJournalProgressMapping : EntityTypeConfiguration<AccountCampaignJournalProgress>
    {
        public AccountCampaignJournalProgressMapping(string schema = "dbo")
        {
            ToTable(schema + ".AccountCampaignJournalProgress");
            HasKey(x => new { x.AccountID, x.CampaignID, x.JournalID });

            Property(x => x.AccountID).HasColumnName("AccountID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.CampaignID).HasColumnName("CampaignID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.JournalID).HasColumnName("JournalID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.IsUnlocked).HasColumnName("IsUnlocked").IsRequired();

            // Foreign keys
            HasRequired(a => a.Account).WithMany(b => b.AccountCampaignJournalProgresses).HasForeignKey(c => c.AccountID); // FK_AccountCampaignJournalProgress_Account
            HasRequired(a => a.CampaignJournal).WithMany(b => b.AccountCampaignJournalProgresses).HasForeignKey(c => new { c.CampaignID, c.JournalID }); // FK_AccountCampaignJournalProgress_CampaignJournal
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
