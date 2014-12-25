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
    // PlanetFaction
    internal partial class PlanetFactionMapping : EntityTypeConfiguration<PlanetFaction>
    {
        public PlanetFactionMapping(string schema = "dbo")
        {
            ToTable(schema + ".PlanetFaction");
            HasKey(x => new { x.PlanetID, x.FactionID });

            Property(x => x.PlanetID).HasColumnName("PlanetID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.FactionID).HasColumnName("FactionID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.Influence).HasColumnName("Influence").IsRequired();
            Property(x => x.Dropships).HasColumnName("Dropships").IsRequired();
            Property(x => x.DropshipsLastAdded).HasColumnName("DropshipsLastAdded").IsOptional();

            // Foreign keys
            HasRequired(a => a.Planet).WithMany(b => b.PlanetFactions).HasForeignKey(c => c.PlanetID); // FK_PlanetFactionInfluence_Planet
            HasRequired(a => a.Faction).WithMany(b => b.PlanetFactions).HasForeignKey(c => c.FactionID); // FK_PlanetFactionInfluence_Faction
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
