namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("MarketOffer")]
    public partial class MarketOffer
    {
        [Key]
        public int OfferID { get; set; }

        public int AccountID { get; set; }

        public int PlanetID { get; set; }

        public int Quantity { get; set; }

        public int Price { get; set; }

        public bool IsSell { get; set; }

        public DateTime? DatePlaced { get; set; }

        public DateTime? DateAccepted { get; set; }

        public int? AcceptedAccountID { get; set; }

        public virtual Account Account { get; set; }

        public virtual Account Account1 { get; set; }

        public virtual Planet Planet { get; set; }
    }
}
