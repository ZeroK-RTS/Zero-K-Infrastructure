using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZkData
{
    public partial class AccountBattleAward
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int AccountID { get; set; }

        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int SpringBattleID { get; set; }

        [Key]
        [Column(Order = 2)]
        [StringLength(50)]
        public string AwardKey { get; set; }

        [StringLength(500)]
        public string AwardDescription { get; set; }

        public double? Value { get; set; }

        public virtual Account Account { get; set; }

        public virtual SpringBattle SpringBattle { get; set; }
    }
}
