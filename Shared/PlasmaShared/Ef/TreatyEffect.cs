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
    // TreatyEffect
    public partial class TreatyEffect
    {
        public int TreatyEffectID { get; set; } // TreatyEffectID (Primary key)
        public int FactionTreatyID { get; set; } // FactionTreatyID
        public int EffectTypeID { get; set; } // EffectTypeID
        public int GivingFactionID { get; set; } // GivingFactionID
        public int ReceivingFactionID { get; set; } // ReceivingFactionID
        public double? Value { get; set; } // Value
        public int? PlanetID { get; set; } // PlanetID

        // Foreign keys
        public virtual Faction Faction_GivingFactionID { get; set; } // FK_TreatyEffect_Faction
        public virtual Faction Faction_ReceivingFactionID { get; set; } // FK_TreatyEffect_Faction1
        public virtual FactionTreaty FactionTreaty { get; set; } // FK_TreatyEffect_FactionTreaty
        public virtual Planet Planet { get; set; } // FK_TreatyEffect_Planet
        public virtual TreatyEffectType TreatyEffectType { get; set; } // FK_TreatyEffect_EffectType
    }

}
