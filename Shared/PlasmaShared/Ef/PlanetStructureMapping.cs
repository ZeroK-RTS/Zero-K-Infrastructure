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
    // PlanetStructure
    internal partial class PlanetStructureMapping : EntityTypeConfiguration<PlanetStructure>
    {
        public PlanetStructureMapping(string schema = "dbo")
        {
            ToTable(schema + ".PlanetStructure");
            HasKey(x => new { x.PlanetID, x.StructureTypeID });

            Property(x => x.PlanetID).HasColumnName("PlanetID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.StructureTypeID).HasColumnName("StructureTypeID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.OwnerAccountID).HasColumnName("OwnerAccountID").IsOptional();
            Property(x => x.ActivatedOnTurn).HasColumnName("ActivatedOnTurn").IsOptional();
            Property(x => x.EnergyPriority).HasColumnName("EnergyPriority").IsRequired();
            Property(x => x.IsActive).HasColumnName("IsActive").IsRequired();
            Property(x => x.TargetPlanetID).HasColumnName("TargetPlanetID").IsOptional();

            // Foreign keys
            HasRequired(a => a.Planet).WithMany(b => b.PlanetStructures).HasForeignKey(c => c.PlanetID); // FK_PlanetStructure_Planet
            HasRequired(a => a.StructureType).WithMany(b => b.PlanetStructures).HasForeignKey(c => c.StructureTypeID); // FK_PlanetStructure_StructureType
            HasOptional(a => a.Account).WithMany(b => b.PlanetStructures).HasForeignKey(c => c.OwnerAccountID); // FK_PlanetStructure_Account
            HasOptional(a => a.PlanetByTargetPlanetID).WithMany(b => b.PlanetStructuresByTargetPlanetID).HasForeignKey(c => c.TargetPlanetID); // FK_PlanetStructure_Planet1
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
