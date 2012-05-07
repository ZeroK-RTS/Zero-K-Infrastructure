using System.Linq;

namespace ZkData
{
    partial class Commander
    {
        public int GetTotalMorphLevelCost(int level) {
            var cost = 0;
            if (Unlock != null) {
                switch (level) {
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