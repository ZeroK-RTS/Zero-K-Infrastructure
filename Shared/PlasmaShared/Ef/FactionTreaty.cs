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

        public TreatyState TreatyState { get; set; }

        public int? TurnsTotal { get; set; }

        [StringLength(1000)]
        public string TreatyNote { get; set; }

        public virtual Account AccountByProposingAccountID { get; set; }

        public virtual Account AccountByAcceptedAccountID { get; set; }

        public virtual Faction FactionByProposingFactionID { get; set; }

        public virtual Faction FactionByAcceptingFactionID { get; set; }

        
        public virtual ICollection<TreatyEffect> TreatyEffects { get; set; }
    }
}
