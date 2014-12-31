namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("CommanderDecorationSlot")]
    public partial class CommanderDecorationSlot
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
