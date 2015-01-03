using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ZkData
{
    public partial class TreatyEffectType
    {
        
        public TreatyEffectType()
        {
            TreatyEffects = new HashSet<TreatyEffect>();
        }

        [Key]
        public int EffectTypeID { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        [StringLength(200)]
        public string Description { get; set; }

        public bool HasValue { get; set; }

        public double? MinValue { get; set; }

        public double? MaxValue { get; set; }

        public bool IsPlanetBased { get; set; }

        public bool IsOneTimeOnly { get; set; }

        public bool? EffectBalanceSameSide { get; set; }

        public bool? EffectPreventInfluenceSpread { get; set; }

        public bool? EffectPreventDropshipAttack { get; set; }

        public bool? EffectPreventBomberAttack { get; set; }

        public bool? EffectAllowDropshipPass { get; set; }

        public bool? EffectAllowBomberPass { get; set; }

        public bool? EffectGiveMetal { get; set; }

        public bool? EffectGiveDropships { get; set; }

        public bool? EffectGiveBombers { get; set; }

        public bool? EffectGiveEnergy { get; set; }

        public bool? EffectShareTechs { get; set; }

        public bool? EffectGiveWarps { get; set; }

        public bool? EffectPreventIngamePwStructureDestruction { get; set; }

        public bool? EffectGiveInfluence { get; set; }

        
        public virtual ICollection<TreatyEffect> TreatyEffects { get; set; }
    }
}
