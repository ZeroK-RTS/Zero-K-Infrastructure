namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("CommanderDecoration")]
    public partial class CommanderDecoration
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int CommanderID { get; set; }

        public int DecorationUnlockID { get; set; }

        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int SlotID { get; set; }

        public virtual Commander Commander { get; set; }

        public virtual CommanderDecorationSlot CommanderDecorationSlot { get; set; }

        public virtual Unlock Unlock { get; set; }
    }
}
