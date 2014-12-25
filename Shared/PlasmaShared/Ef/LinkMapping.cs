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
    // Link
    internal partial class LinkMapping : EntityTypeConfiguration<Link>
    {
        public LinkMapping(string schema = "dbo")
        {
            ToTable(schema + ".Link");
            HasKey(x => new { x.PlanetID1, x.PlanetID2 });

            Property(x => x.PlanetID1).HasColumnName("PlanetID1").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.PlanetID2).HasColumnName("PlanetID2").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.GalaxyID).HasColumnName("GalaxyID").IsRequired();

            // Foreign keys
            HasRequired(a => a.Planet_PlanetID1).WithMany(b => b.Links_PlanetID1).HasForeignKey(c => c.PlanetID1); // FK_Link_Planet
            HasRequired(a => a.Planet_PlanetID2).WithMany(b => b.Links_PlanetID2).HasForeignKey(c => c.PlanetID2); // FK_Link_Planet1
            HasRequired(a => a.Galaxy).WithMany(b => b.Links).HasForeignKey(c => c.GalaxyID); // FK_Link_Galaxy
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
