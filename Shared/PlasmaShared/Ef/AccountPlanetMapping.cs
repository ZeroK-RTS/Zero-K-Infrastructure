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
    // AccountPlanet
    internal partial class AccountPlanetMapping : EntityTypeConfiguration<AccountPlanet>
    {
        public AccountPlanetMapping(string schema = "dbo")
        {
            ToTable(schema + ".AccountPlanet");
            HasKey(x => new { x.PlanetID, x.AccountID });

            Property(x => x.PlanetID).HasColumnName("PlanetID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.AccountID).HasColumnName("AccountID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.AttackPoints).HasColumnName("AttackPoints").IsRequired();

            // Foreign keys
            HasRequired(a => a.Planet).WithMany(b => b.AccountPlanets).HasForeignKey(c => c.PlanetID); // FK_PlayerPlanet_Planet
            HasRequired(a => a.Account).WithMany(b => b.AccountPlanets).HasForeignKey(c => c.AccountID); // FK_PlayerPlanet_Player
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
