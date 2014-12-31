namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Commander")]
    public partial class Commander
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
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

        public virtual Account Account { get; set; }

        public virtual Unlock Unlock { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<CommanderDecoration> CommanderDecorations { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<CommanderModule> CommanderModules { get; set; }
    }
}
