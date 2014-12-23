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
    // FactionTreaty
    public partial class FactionTreaty
    {
        public int FactionTreatyID { get; set; } // FactionTreatyID (Primary key)
        public int ProposingFactionID { get; set; } // ProposingFactionID
        public int ProposingAccountID { get; set; } // ProposingAccountID
        public int AcceptingFactionID { get; set; } // AcceptingFactionID
        public int? AcceptedAccountID { get; set; } // AcceptedAccountID
        public int? TurnsRemaining { get; set; } // TurnsRemaining
        public int TreatyState { get; set; } // TreatyState
        public int? TurnsTotal { get; set; } // TurnsTotal
        public string TreatyNote { get; set; } // TreatyNote

        // Reverse navigation
        public virtual ICollection<TreatyEffect> TreatyEffects { get; set; } // TreatyEffect.FK_TreatyEffect_FactionTreaty

        // Foreign keys
        public virtual Account Account_AcceptedAccountID { get; set; } // FK_FactionTreaty_Account1
        public virtual Account Account_ProposingAccountID { get; set; } // FK_FactionTreaty_Account
        public virtual Faction Faction_AcceptingFactionID { get; set; } // FK_FactionTreaty_Faction1
        public virtual Faction Faction_ProposingFactionID { get; set; } // FK_FactionTreaty_Faction

        public FactionTreaty()
        {
            TreatyState = 0;
            TreatyEffects = new List<TreatyEffect>();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
