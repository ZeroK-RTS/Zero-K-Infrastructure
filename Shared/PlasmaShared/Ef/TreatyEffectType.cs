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
    // TreatyEffectType
    public partial class TreatyEffectType
    {
        public int EffectTypeID { get; set; } // EffectTypeID (Primary key)
        public string Name { get; set; } // Name
        public string Description { get; set; } // Description
        public bool HasValue { get; set; } // HasValue
        public double? MinValue { get; set; } // MinValue
        public double? MaxValue { get; set; } // MaxValue
        public bool IsPlanetBased { get; set; } // IsPlanetBased
        public bool IsOneTimeOnly { get; set; } // IsOneTimeOnly
        public bool? EffectBalanceSameSide { get; set; } // EffectBalanceSameSide
        public bool? EffectPreventInfluenceSpread { get; set; } // EffectPreventInfluenceSpread
        public bool? EffectPreventDropshipAttack { get; set; } // EffectPreventDropshipAttack
        public bool? EffectPreventBomberAttack { get; set; } // EffectPreventBomberAttack
        public bool? EffectAllowDropshipPass { get; set; } // EffectAllowDropshipPass
        public bool? EffectAllowBomberPass { get; set; } // EffectAllowBomberPass
        public bool? EffectGiveMetal { get; set; } // EffectGiveMetal
        public bool? EffectGiveDropships { get; set; } // EffectGiveDropships
        public bool? EffectGiveBombers { get; set; } // EffectGiveBombers
        public bool? EffectGiveEnergy { get; set; } // EffectGiveEnergy
        public bool? EffectShareTechs { get; set; } // EffectShareTechs
        public bool? EffectGiveWarps { get; set; } // EffectGiveWarps
        public bool? EffectPreventIngamePwStructureDestruction { get; set; } // EffectPreventIngamePwStructureDestruction
        public bool? EffectGiveInfluence { get; set; } // EffectGiveInfluence

        // Reverse navigation
        public virtual ICollection<TreatyEffect> TreatyEffects { get; set; } // TreatyEffect.FK_TreatyEffect_EffectType

        public TreatyEffectType()
        {
            HasValue = false;
            IsPlanetBased = false;
            IsOneTimeOnly = false;
            TreatyEffects = new List<TreatyEffect>();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
