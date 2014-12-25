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
    // Faction
    internal partial class FactionMapping : EntityTypeConfiguration<Faction>
    {
        public FactionMapping(string schema = "dbo")
        {
            ToTable(schema + ".Faction");
            HasKey(x => x.FactionID);

            Property(x => x.FactionID).HasColumnName("FactionID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.Name).HasColumnName("Name").IsRequired().HasMaxLength(200);
            Property(x => x.Shortcut).HasColumnName("Shortcut").IsRequired().HasMaxLength(50);
            Property(x => x.Color).HasColumnName("Color").IsRequired().IsUnicode(false).HasMaxLength(20);
            Property(x => x.IsDeleted).HasColumnName("IsDeleted").IsRequired();
            Property(x => x.Metal).HasColumnName("Metal").IsRequired();
            Property(x => x.Dropships).HasColumnName("Dropships").IsRequired();
            Property(x => x.Bombers).HasColumnName("Bombers").IsRequired();
            Property(x => x.SecretTopic).HasColumnName("SecretTopic").IsOptional().HasMaxLength(500);
            Property(x => x.EnergyProducedLastTurn).HasColumnName("EnergyProducedLastTurn").IsRequired();
            Property(x => x.EnergyDemandLastTurn).HasColumnName("EnergyDemandLastTurn").IsRequired();
            Property(x => x.Warps).HasColumnName("Warps").IsRequired();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
