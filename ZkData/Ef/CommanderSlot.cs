using System.Collections.Generic;

namespace ZkData
{
    public partial class CommanderSlot
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
