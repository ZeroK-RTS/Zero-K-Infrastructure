namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("CommanderSlot")]
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
