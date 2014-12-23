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
    // PlanetOwnerHistory
    internal partial class PlanetOwnerHistoryMapping : EntityTypeConfiguration<PlanetOwnerHistory>
    {
        public PlanetOwnerHistoryMapping(string schema = "dbo")
        {
            ToTable(schema + ".PlanetOwnerHistory");
            HasKey(x => new { x.PlanetID, x.Turn });

            Property(x => x.PlanetID).HasColumnName("PlanetID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.Turn).HasColumnName("Turn").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.OwnerAccountID).HasColumnName("OwnerAccountID").IsOptional();
            Property(x => x.OwnerClanID).HasColumnName("OwnerClanID").IsOptional();
            Property(x => x.OwnerFactionID).HasColumnName("OwnerFactionID").IsOptional();

            // Foreign keys
            HasRequired(a => a.Planet).WithMany(b => b.PlanetOwnerHistories).HasForeignKey(c => c.PlanetID); // FK_PlanetOwnerHistory_Planet
            HasOptional(a => a.Account).WithMany(b => b.PlanetOwnerHistories).HasForeignKey(c => c.OwnerAccountID); // FK_PlanetOwnerHistory_Account
            HasOptional(a => a.Clan).WithMany(b => b.PlanetOwnerHistories).HasForeignKey(c => c.OwnerClanID); // FK_PlanetOwnerHistory_Clan
            HasOptional(a => a.Faction).WithMany(b => b.PlanetOwnerHistories).HasForeignKey(c => c.OwnerFactionID); // FK_PlanetOwnerHistory_Faction
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
