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
    public partial class PlanetOwnerHistory
    {
        public int PlanetID { get; set; } // PlanetID (Primary key)
        public int Turn { get; set; } // Turn (Primary key)
        public int? OwnerAccountID { get; set; } // OwnerAccountID
        public int? OwnerClanID { get; set; } // OwnerClanID
        public int? OwnerFactionID { get; set; } // OwnerFactionID

        // Foreign keys
        public virtual Account Account { get; set; } // FK_PlanetOwnerHistory_Account
        public virtual Clan Clan { get; set; } // FK_PlanetOwnerHistory_Clan
        public virtual Faction Faction { get; set; } // FK_PlanetOwnerHistory_Faction
        public virtual Planet Planet { get; set; } // FK_PlanetOwnerHistory_Planet
    }

}
