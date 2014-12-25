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
    // CampaignLink
    internal partial class CampaignLinkMapping : EntityTypeConfiguration<CampaignLink>
    {
        public CampaignLinkMapping(string schema = "dbo")
        {
            ToTable(schema + ".CampaignLink");
            HasKey(x => new { x.PlanetToUnlockID, x.UnlockingPlanetID });

            Property(x => x.PlanetToUnlockID).HasColumnName("PlanetToUnlockID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.UnlockingPlanetID).HasColumnName("UnlockingPlanetID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.CampaignID).HasColumnName("CampaignID").IsRequired();

            // Foreign keys
            HasRequired(a => a.PlanetToUnlock).WithMany(b => b.CampaignLinks_CampaignID).HasForeignKey(c => new { c.PlanetToUnlockID, c.CampaignID }); // FK_CampaignLink_CampaignPlanet
            HasRequired(a => a.UnlockingPlanet).WithMany(b => b.CampaignLinks1).HasForeignKey(c => new { c.UnlockingPlanetID, c.CampaignID }); // FK_CampaignLink_CampaignPlanet1
            HasRequired(a => a.Campaign).WithMany(b => b.CampaignLinks).HasForeignKey(c => c.CampaignID); // FK_CampaignLink_Campaign
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
