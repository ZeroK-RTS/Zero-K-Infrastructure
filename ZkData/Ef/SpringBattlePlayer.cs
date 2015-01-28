using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZkData
{
    public class SpringBattlePlayer
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int SpringBattleID { get; set; }
        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int AccountID { get; set; }
        public bool IsSpectator { get; set; }
        public bool IsInVictoryTeam { get; set; }
        [StringLength(50)]
        public string CommanderType { get; set; }
        public int? LoseTime { get; set; }
        public int AllyNumber { get; set; }
        public float? EloChange { get; set; }
        public int? XpChange { get; set; }
        public int? Influence { get; set; }

        public virtual Account Account { get; set; }
        public virtual SpringBattle SpringBattle { get; set; }
    }
}
