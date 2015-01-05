using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZkData
{
    public class CommanderDecorationSlot
    {
        
        public CommanderDecorationSlot()
        {
            CommanderDecorations = new HashSet<CommanderDecoration>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int CommanderDecorationSlotID { get; set; }
        
        public virtual ICollection<CommanderDecoration> CommanderDecorations { get; set; }
    }
}
