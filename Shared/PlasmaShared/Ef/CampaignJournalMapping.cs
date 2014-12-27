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
    // CampaignJournal
    internal partial class CampaignJournalMapping : EntityTypeConfiguration<CampaignJournal>
    {
        public CampaignJournalMapping(string schema = "dbo")
        {
            ToTable(schema + ".CampaignJournal");
            HasKey(x => new { x.CampaignID, x.JournalID });

            Property(x => x.CampaignID).HasColumnName("CampaignID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.JournalID).HasColumnName("JournalID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.PlanetID).HasColumnName("PlanetID").IsOptional();
            Property(x => x.UnlockOnPlanetUnlock).HasColumnName("UnlockOnPlanetUnlock").IsRequired();
            Property(x => x.UnlockOnPlanetCompletion).HasColumnName("UnlockOnPlanetCompletion").IsRequired();
            Property(x => x.StartsUnlocked).HasColumnName("StartsUnlocked").IsRequired();
            Property(x => x.Title).HasColumnName("Title").IsRequired().HasMaxLength(50);
            Property(x => x.Text).HasColumnName("Text").IsRequired();
            Property(x => x.Category).HasColumnName("Category").IsOptional();

            // Foreign keys
            HasRequired(a => a.Planet).WithMany(b => b.CampaignJournals).HasForeignKey(c => new { c.CampaignID, c.PlanetID }).WillCascadeOnDelete(false); // FK_CampaignJournal_CampaignPlanet

            HasRequired(x=>x.Campaign).WithMany(x=>x.CampaignJournals).HasForeignKey(x=>x.CampaignID).WillCascadeOnDelete(false);

            InitializePartial();
        }
        partial void InitializePartial();
    }

}
