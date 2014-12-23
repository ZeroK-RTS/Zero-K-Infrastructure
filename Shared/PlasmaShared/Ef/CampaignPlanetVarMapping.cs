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
    // CampaignPlanetVar
    internal partial class CampaignPlanetVarMapping : EntityTypeConfiguration<CampaignPlanetVar>
    {
        public CampaignPlanetVarMapping(string schema = "dbo")
        {
            ToTable(schema + ".CampaignPlanetVar");
            HasKey(x => new { x.CampaignID, x.PlanetID, x.RequiredVarID });

            Property(x => x.CampaignID).HasColumnName("CampaignID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.PlanetID).HasColumnName("PlanetID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.RequiredVarID).HasColumnName("RequiredVarID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.RequiredValue).HasColumnName("RequiredValue").IsRequired();

            // Foreign keys
            HasRequired(a => a.CampaignVar).WithMany(b => b.CampaignPlanetVars).HasForeignKey(c => new { c.CampaignID, c.RequiredVarID }); // FK_CampaignPlanetVar_CampaignVar
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
