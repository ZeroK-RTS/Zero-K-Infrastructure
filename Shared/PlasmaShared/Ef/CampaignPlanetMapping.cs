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
    // CampaignPlanet
    internal partial class CampaignPlanetMapping : EntityTypeConfiguration<CampaignPlanet>
    {
        public CampaignPlanetMapping(string schema = "dbo")
        {
            ToTable(schema + ".CampaignPlanet");
            HasKey(x => new { x.PlanetID, x.CampaignID });

            Property(x => x.PlanetID).HasColumnName("PlanetID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.Name).HasColumnName("Name").IsRequired().IsUnicode(false).HasMaxLength(50);
            Property(x => x.MissionID).HasColumnName("MissionID").IsRequired();
            Property(x => x.CampaignID).HasColumnName("CampaignID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.X).HasColumnName("X").IsRequired();
            Property(x => x.Y).HasColumnName("Y").IsRequired();
            Property(x => x.IsSkirmish).HasColumnName("IsSkirmish").IsRequired();
            Property(x => x.Description).HasColumnName("Description").IsOptional();
            Property(x => x.DescriptionStory).HasColumnName("DescriptionStory").IsOptional();
            Property(x => x.StartsUnlocked).HasColumnName("StartsUnlocked").IsRequired();
            Property(x => x.HideIfLocked).HasColumnName("HideIfLocked").IsRequired();
            Property(x => x.DisplayedMap).HasColumnName("DisplayedMap").IsOptional().HasMaxLength(100);

            // Foreign keys
            HasRequired(a => a.Mission).WithMany(b => b.CampaignPlanets).HasForeignKey(c => c.MissionID); // FK_CampaignPlanet_Mission
            HasRequired(a => a.Campaign).WithMany(b => b.CampaignPlanets).HasForeignKey(c => c.CampaignID); // FK_CampaignPlanet_Campaign
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
