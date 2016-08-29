using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ZkData
{
    public class AccountRelation
    {
        [Column(Order = 0)]
        [Key]
        public int OwnerAccountID { get; set; }

        [Key]
        [Column(Order=1)]
        public int TargetAccountID { get; set; }

        [ForeignKey("OwnerAccountID")]
        public virtual Account Owner { get; set; }

        [ForeignKey("TargetAccountID")]
        public virtual Account Target { get; set; }

        public Relation Relation { get;set;}
    }
}