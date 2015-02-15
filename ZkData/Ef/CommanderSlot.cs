using System.Collections.Generic;

namespace ZkData
{
    public class CommanderSlot
    {
        
        public CommanderSlot()
        {
            CommanderModules = new HashSet<CommanderModule>();
        }

        public int CommanderSlotID { get; set; }
        public int MorphLevel { get; set; }

        public UnlockTypes UnlockType { get; set; }
        public virtual ICollection<CommanderModule> CommanderModules { get; set; }
    }
}
