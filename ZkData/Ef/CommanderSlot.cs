using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

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
        public int? ChassisID { get; set; }

        public UnlockTypes UnlockType { get; set; }

        [ForeignKey("ChassisID")]
        public virtual Unlock Chassis { get; set; }
        public virtual ICollection<CommanderModule> CommanderModules { get; set; }
    }
}
