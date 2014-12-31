namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("FactionTreaty")]
    public partial class FactionTreaty
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public FactionTreaty()
        {
            TreatyEffects = new HashSet<TreatyEffect>();
        }

        public int FactionTreatyID { get; set; }

        public int ProposingFactionID { get; set; }

        public int ProposingAccountID { get; set; }

        public int AcceptingFactionID { get; set; }

        public int? AcceptedAccountID { get; set; }

        public int? TurnsRemaining { get; set; }

        public int TreatyState { get; set; }

        public int? TurnsTotal { get; set; }

        public string TreatyNote { get; set; }

        public virtual Account Account { get; set; }

        public virtual Account Account1 { get; set; }

        public virtual Faction Faction { get; set; }

        public virtual Faction Faction1 { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<TreatyEffect> TreatyEffects { get; set; }
    }
}
