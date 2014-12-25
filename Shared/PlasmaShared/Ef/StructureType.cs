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
    // StructureType
    public partial class StructureType
    {
        public int StructureTypeID { get; set; } // StructureTypeID (Primary key)
        public string Name { get; set; } // Name
        public string Description { get; set; } // Description
        public string IngameUnitName { get; set; } // IngameUnitName
        public string MapIcon { get; set; } // MapIcon
        public string DisabledMapIcon { get; set; } // DisabledMapIcon
        public double? UpkeepEnergy { get; set; } // UpkeepEnergy
        public int? TurnsToActivate { get; set; } // TurnsToActivate
        public double? EffectDropshipProduction { get; set; } // EffectDropshipProduction
        public int? EffectDropshipCapacity { get; set; } // EffectDropshipCapacity
        public double? EffectBomberProduction { get; set; } // EffectBomberProduction
        public int? EffectBomberCapacity { get; set; } // EffectBomberCapacity
        public double? EffectInfluenceSpread { get; set; } // EffectInfluenceSpread
        public int? EffectUnlockID { get; set; } // EffectUnlockID
        public double? EffectEnergyPerTurn { get; set; } // EffectEnergyPerTurn
        public bool? EffectIsVictoryPlanet { get; set; } // EffectIsVictoryPlanet
        public double? EffectWarpProduction { get; set; } // EffectWarpProduction
        public bool? EffectAllowShipTraversal { get; set; } // EffectAllowShipTraversal
        public double? EffectDropshipDefense { get; set; } // EffectDropshipDefense
        public double? EffectBomberDefense { get; set; } // EffectBomberDefense
        public string EffectBots { get; set; } // EffectBots
        public bool? EffectBlocksInfluenceSpread { get; set; } // EffectBlocksInfluenceSpread
        public bool? EffectBlocksJumpgate { get; set; } // EffectBlocksJumpgate
        public double? EffectRemoteInfluenceSpread { get; set; } // EffectRemoteInfluenceSpread
        public bool? EffectCreateLink { get; set; } // EffectCreateLink
        public bool? EffectChangePlanetMap { get; set; } // EffectChangePlanetMap
        public bool? EffectPlanetBuster { get; set; } // EffectPlanetBuster
        public double Cost { get; set; } // Cost
        public bool IsBuildable { get; set; } // IsBuildable
        public bool IsIngameDestructible { get; set; } // IsIngameDestructible
        public bool IsBomberDestructible { get; set; } // IsBomberDestructible
        public bool OwnerChangeDeletesThis { get; set; } // OwnerChangeDeletesThis
        public bool OwnerChangeDisablesThis { get; set; } // OwnerChangeDisablesThis
        public bool BattleDeletesThis { get; set; } // BattleDeletesThis
        public bool IsSingleUse { get; set; } // IsSingleUse
        public bool RequiresPlanetTarget { get; set; } // RequiresPlanetTarget
        public double? EffectReduceBattleInfluenceGain { get; set; } // EffectReduceBattleInfluenceGain

        // Reverse navigation
        public virtual ICollection<PlanetStructure> PlanetStructures { get; set; } // Many to many mapping

        // Foreign keys
        public virtual Unlock Unlock { get; set; } // FK_StructureType_Unlock

        public StructureType()
        {
            UpkeepEnergy = 0;
            IsBuildable = false;
            IsIngameDestructible = true;
            IsBomberDestructible = true;
            OwnerChangeDeletesThis = false;
            OwnerChangeDisablesThis = true;
            BattleDeletesThis = false;
            IsSingleUse = false;
            RequiresPlanetTarget = false;
            PlanetStructures = new List<PlanetStructure>();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
