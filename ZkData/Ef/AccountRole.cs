using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZkData
{
    public partial class AccountRole
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int AccountID { get; set; }

        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int RoleTypeID { get; set; }

        public DateTime Inauguration { get; set; }

        public int? FactionID { get; set; }

        public int? ClanID { get; set; }

        public virtual Account Account { get; set; }

        public virtual Clan Clan { get; set; }

        public virtual Faction Faction { get; set; }

        public virtual RoleType RoleType { get; set; }
    }
}
