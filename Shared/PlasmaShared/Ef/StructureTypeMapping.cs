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
    // StructureType
    internal partial class StructureTypeMapping : EntityTypeConfiguration<StructureType>
    {
        public StructureTypeMapping(string schema = "dbo")
        {
            ToTable(schema + ".StructureType");
            HasKey(x => x.StructureTypeID);

            Property(x => x.StructureTypeID).HasColumnName("StructureTypeID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.Name).HasColumnName("Name").IsRequired().HasMaxLength(50);
            Property(x => x.Description).HasColumnName("Description").IsOptional().HasMaxLength(250);
            Property(x => x.IngameUnitName).HasColumnName("IngameUnitName").IsOptional().HasMaxLength(50);
            Property(x => x.MapIcon).HasColumnName("MapIcon").IsOptional().HasMaxLength(50);
            Property(x => x.DisabledMapIcon).HasColumnName("DisabledMapIcon").IsOptional().HasMaxLength(50);
            Property(x => x.UpkeepEnergy).HasColumnName("UpkeepEnergy").IsOptional();
            Property(x => x.TurnsToActivate).HasColumnName("TurnsToActivate").IsOptional();
            Property(x => x.EffectDropshipProduction).HasColumnName("EffectDropshipProduction").IsOptional();
            Property(x => x.EffectDropshipCapacity).HasColumnName("EffectDropshipCapacity").IsOptional();
            Property(x => x.EffectBomberProduction).HasColumnName("EffectBomberProduction").IsOptional();
            Property(x => x.EffectBomberCapacity).HasColumnName("EffectBomberCapacity").IsOptional();
            Property(x => x.EffectInfluenceSpread).HasColumnName("EffectInfluenceSpread").IsOptional();
            Property(x => x.EffectUnlockID).HasColumnName("EffectUnlockID").IsOptional();
            Property(x => x.EffectEnergyPerTurn).HasColumnName("EffectEnergyPerTurn").IsOptional();
            Property(x => x.EffectIsVictoryPlanet).HasColumnName("EffectIsVictoryPlanet").IsOptional();
            Property(x => x.EffectWarpProduction).HasColumnName("EffectWarpProduction").IsOptional();
            Property(x => x.EffectAllowShipTraversal).HasColumnName("EffectAllowShipTraversal").IsOptional();
            Property(x => x.EffectDropshipDefense).HasColumnName("EffectDropshipDefense").IsOptional();
            Property(x => x.EffectBomberDefense).HasColumnName("EffectBomberDefense").IsOptional();
            Property(x => x.EffectBots).HasColumnName("EffectBots").IsOptional().HasMaxLength(100);
            Property(x => x.EffectBlocksInfluenceSpread).HasColumnName("EffectBlocksInfluenceSpread").IsOptional();
            Property(x => x.EffectBlocksJumpgate).HasColumnName("EffectBlocksJumpgate").IsOptional();
            Property(x => x.EffectRemoteInfluenceSpread).HasColumnName("EffectRemoteInfluenceSpread").IsOptional();
            Property(x => x.EffectCreateLink).HasColumnName("EffectCreateLink").IsOptional();
            Property(x => x.EffectChangePlanetMap).HasColumnName("EffectChangePlanetMap").IsOptional();
            Property(x => x.EffectPlanetBuster).HasColumnName("EffectPlanetBuster").IsOptional();
            Property(x => x.Cost).HasColumnName("Cost").IsRequired();
            Property(x => x.IsBuildable).HasColumnName("IsBuildable").IsRequired();
            Property(x => x.IsIngameDestructible).HasColumnName("IsIngameDestructible").IsRequired();
            Property(x => x.IsBomberDestructible).HasColumnName("IsBomberDestructible").IsRequired();
            Property(x => x.OwnerChangeDeletesThis).HasColumnName("OwnerChangeDeletesThis").IsRequired();
            Property(x => x.OwnerChangeDisablesThis).HasColumnName("OwnerChangeDisablesThis").IsRequired();
            Property(x => x.BattleDeletesThis).HasColumnName("BattleDeletesThis").IsRequired();
            Property(x => x.IsSingleUse).HasColumnName("IsSingleUse").IsRequired();
            Property(x => x.RequiresPlanetTarget).HasColumnName("RequiresPlanetTarget").IsRequired();
            Property(x => x.EffectReduceBattleInfluenceGain).HasColumnName("EffectReduceBattleInfluenceGain").IsOptional();

            // Foreign keys
            HasOptional(a => a.Unlock).WithMany(b => b.StructureTypes).HasForeignKey(c => c.EffectUnlockID); // FK_StructureType_Unlock
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
