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
    // TreatyEffect
    internal partial class TreatyEffectMapping : EntityTypeConfiguration<TreatyEffect>
    {
        public TreatyEffectMapping(string schema = "dbo")
        {
            ToTable(schema + ".TreatyEffect");
            HasKey(x => x.TreatyEffectID);

            Property(x => x.TreatyEffectID).HasColumnName("TreatyEffectID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.FactionTreatyID).HasColumnName("FactionTreatyID").IsRequired();
            Property(x => x.EffectTypeID).HasColumnName("EffectTypeID").IsRequired();
            Property(x => x.GivingFactionID).HasColumnName("GivingFactionID").IsRequired();
            Property(x => x.ReceivingFactionID).HasColumnName("ReceivingFactionID").IsRequired();
            Property(x => x.Value).HasColumnName("Value").IsOptional();
            Property(x => x.PlanetID).HasColumnName("PlanetID").IsOptional();

            // Foreign keys
            HasRequired(a => a.FactionTreaty).WithMany(b => b.TreatyEffects).HasForeignKey(c => c.FactionTreatyID); // FK_TreatyEffect_FactionTreaty
            HasRequired(a => a.TreatyEffectType).WithMany(b => b.TreatyEffects).HasForeignKey(c => c.EffectTypeID); // FK_TreatyEffect_EffectType
            HasRequired(a => a.Faction_GivingFactionID).WithMany(b => b.TreatyEffects_GivingFactionID).HasForeignKey(c => c.GivingFactionID); // FK_TreatyEffect_Faction
            HasRequired(a => a.Faction_ReceivingFactionID).WithMany(b => b.TreatyEffects_ReceivingFactionID).HasForeignKey(c => c.ReceivingFactionID); // FK_TreatyEffect_Faction1
            HasOptional(a => a.Planet).WithMany(b => b.TreatyEffects).HasForeignKey(c => c.PlanetID); // FK_TreatyEffect_Planet
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
