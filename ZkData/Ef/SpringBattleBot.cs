using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ZkData
{
    public class SpringBattleBot
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int SpringBattleID { get; set; }
        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string BotAI { get; set; }
        public string BotName { get; set; }

        public bool IsInVictoryTeam { get; set; }
        public int AllyNumber { get; set; }

        public virtual SpringBattle SpringBattle { get; set; }
    }
}