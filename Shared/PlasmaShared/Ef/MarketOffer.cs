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
    // MarketOffer
    public partial class MarketOffer
    {
        public int OfferID { get; set; } // OfferID (Primary key)
        public int AccountID { get; set; } // AccountID
        public int PlanetID { get; set; } // PlanetID
        public int Quantity { get; set; } // Quantity
        public int Price { get; set; } // Price
        public bool IsSell { get; set; } // IsSell
        public DateTime? DatePlaced { get; set; } // DatePlaced
        public DateTime? DateAccepted { get; set; } // DateAccepted
        public int? AcceptedAccountID { get; set; } // AcceptedAccountID

        // Foreign keys
        public virtual Account Account_AcceptedAccountID { get; set; } // FK_MarketOffer_Player1
        public virtual Account Account_AccountID { get; set; } // FK_MarketOffer_Player
        public virtual Planet Planet { get; set; } // FK_MarketOffer_Planet
    }

}
