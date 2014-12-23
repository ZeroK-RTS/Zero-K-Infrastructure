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
    // TreatyEffectType
    internal partial class TreatyEffectTypeMapping : EntityTypeConfiguration<TreatyEffectType>
    {
        public TreatyEffectTypeMapping(string schema = "dbo")
        {
            ToTable(schema + ".TreatyEffectType");
            HasKey(x => x.EffectTypeID);

            Property(x => x.EffectTypeID).HasColumnName("EffectTypeID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.Name).HasColumnName("Name").IsRequired().HasMaxLength(50);
            Property(x => x.Description).HasColumnName("Description").IsRequired().HasMaxLength(200);
            Property(x => x.HasValue).HasColumnName("HasValue").IsRequired();
            Property(x => x.MinValue).HasColumnName("MinValue").IsOptional();
            Property(x => x.MaxValue).HasColumnName("MaxValue").IsOptional();
            Property(x => x.IsPlanetBased).HasColumnName("IsPlanetBased").IsRequired();
            Property(x => x.IsOneTimeOnly).HasColumnName("IsOneTimeOnly").IsRequired();
            Property(x => x.EffectBalanceSameSide).HasColumnName("EffectBalanceSameSide").IsOptional();
            Property(x => x.EffectPreventInfluenceSpread).HasColumnName("EffectPreventInfluenceSpread").IsOptional();
            Property(x => x.EffectPreventDropshipAttack).HasColumnName("EffectPreventDropshipAttack").IsOptional();
            Property(x => x.EffectPreventBomberAttack).HasColumnName("EffectPreventBomberAttack").IsOptional();
            Property(x => x.EffectAllowDropshipPass).HasColumnName("EffectAllowDropshipPass").IsOptional();
            Property(x => x.EffectAllowBomberPass).HasColumnName("EffectAllowBomberPass").IsOptional();
            Property(x => x.EffectGiveMetal).HasColumnName("EffectGiveMetal").IsOptional();
            Property(x => x.EffectGiveDropships).HasColumnName("EffectGiveDropships").IsOptional();
            Property(x => x.EffectGiveBombers).HasColumnName("EffectGiveBombers").IsOptional();
            Property(x => x.EffectGiveEnergy).HasColumnName("EffectGiveEnergy").IsOptional();
            Property(x => x.EffectShareTechs).HasColumnName("EffectShareTechs").IsOptional();
            Property(x => x.EffectGiveWarps).HasColumnName("EffectGiveWarps").IsOptional();
            Property(x => x.EffectPreventIngamePwStructureDestruction).HasColumnName("EffectPreventIngamePwStructureDestruction").IsOptional();
            Property(x => x.EffectGiveInfluence).HasColumnName("EffectGiveInfluence").IsOptional();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
