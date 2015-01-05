using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ZkData
{
    public class Commander
    {
        public Commander()
        {
            CommanderDecorations = new HashSet<CommanderDecoration>();
            CommanderModules = new HashSet<CommanderModule>();
        }

        public int CommanderID { get; set; }
        public int AccountID { get; set; }
        public int ProfileNumber { get; set; }
        [StringLength(200)]
        public string Name { get; set; }
        public int ChassisUnlockID { get; set; }

        public virtual Account AccountByAccountID { get; set; }
        public virtual Unlock Unlock { get; set; }
        public virtual ICollection<CommanderDecoration> CommanderDecorations { get; set; }
        public virtual ICollection<CommanderModule> CommanderModules { get; set; }

        public int GetTotalMorphLevelCost(int level)
        {
            var cost = 0;
            if (Unlock != null)
            {
                switch (level)
                {
                    case 5:
                        cost += Unlock.MetalCostMorph5 ?? 0;
                        goto case 4;
                    case 4:
                        cost += Unlock.MetalCostMorph4 ?? 0;
                        goto case 3;
                    case 3:
                        cost += Unlock.MetalCostMorph3 ?? 0;
                        goto case 2;
                    case 2:
                        cost += Unlock.MetalCostMorph2 ?? 0;
                        goto case 1;
                    case 1:
                        cost += Unlock.MetalCost ?? 0;
                        break;
                }
            }
            cost += CommanderModules.Where(x => x.CommanderSlot.MorphLevel <= level && x.Unlock != null).Sum(x => (int?)x.Unlock.MetalCost) ?? 0;
            return cost;
        }
    }
}
