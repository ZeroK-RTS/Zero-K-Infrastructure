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
    // CampaignEvent
    internal partial class CampaignEventMapping : EntityTypeConfiguration<CampaignEvent>
    {
        public CampaignEventMapping(string schema = "dbo")
        {
            ToTable(schema + ".CampaignEvent");
            HasKey(x => x.EventID);

            Property(x => x.AccountID).HasColumnName("AccountID").IsRequired();
            Property(x => x.CampaignID).HasColumnName("CampaignID").IsRequired();
            Property(x => x.EventID).HasColumnName("EventID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.PlanetID).HasColumnName("PlanetID").IsOptional();
            Property(x => x.Text).HasColumnName("Text").IsRequired().HasMaxLength(4000);
            Property(x => x.Time).HasColumnName("Time").IsRequired();
            Property(x => x.PlainText).HasColumnName("PlainText").IsOptional().HasMaxLength(4000);

            // Foreign keys
            HasRequired(a => a.Account).WithMany(b => b.CampaignEvents).HasForeignKey(c => c.AccountID); // FK_CampaignEvent_Account
            HasRequired(a => a.CampaignPlanet).WithMany(b => b.CampaignEvents).HasForeignKey(c => new { c.CampaignID, c.PlanetID }).WillCascadeOnDelete(false); // FK_CampaignEvent_CampaignPlanet
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
