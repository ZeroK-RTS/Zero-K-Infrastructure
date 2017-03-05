using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ZkData
{
    public class SpringBattleBot
    {
        [Key]
        public int SpringBattleBotID { get; set; }

        public int SpringBattleID { get; set; }

        public string BotAI { get; set; }
        public string BotName { get; set; }

        public bool IsInVictoryTeam { get; set; }
        public int AllyNumber { get; set; }

        [ForeignKey(nameof(SpringBattleID))]
        public virtual SpringBattle SpringBattle { get; set; }
    }
}