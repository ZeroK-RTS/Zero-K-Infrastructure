using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZkData
{
    public class PlanetOwnerHistory
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int PlanetID { get; set; }
        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Turn { get; set; }
        public int? OwnerAccountID { get; set; }
        public int? OwnerClanID { get; set; }
        public int? OwnerFactionID { get; set; }

        public virtual Account Account { get; set; }
        public virtual Clan Clan { get; set; }
        public virtual Faction Faction { get; set; }
        public virtual Planet Planet { get; set; }
    }
}
