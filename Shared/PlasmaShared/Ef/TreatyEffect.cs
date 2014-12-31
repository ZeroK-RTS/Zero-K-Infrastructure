namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("TreatyEffect")]
    public partial class TreatyEffect
    {
        public int TreatyEffectID { get; set; }

        public int FactionTreatyID { get; set; }

        public int EffectTypeID { get; set; }

        public int GivingFactionID { get; set; }

        public int ReceivingFactionID { get; set; }

        public double? Value { get; set; }

        public int? PlanetID { get; set; }

        public virtual Faction FactionByGivingFactionID { get; set; }

        public virtual Faction FactionByReceivingFactionID { get; set; }

        public virtual FactionTreaty FactionTreaty { get; set; }

        public virtual Planet Planet { get; set; }

        public virtual TreatyEffectType TreatyEffectType { get; set; }
    }
}
