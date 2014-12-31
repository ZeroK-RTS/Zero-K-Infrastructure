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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public CommanderSlot()
        {
            CommanderModules = new HashSet<CommanderModule>();
        }

        public int CommanderSlotID { get; set; }

        public int MorphLevel { get; set; }

        public int UnlockType { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<CommanderModule> CommanderModules { get; set; }
    }
}
