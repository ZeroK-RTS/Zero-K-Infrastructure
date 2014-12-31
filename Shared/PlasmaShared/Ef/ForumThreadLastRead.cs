namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("ForumThreadLastRead")]
    public partial class ForumThreadLastRead
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ForumThreadID { get; set; }

        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int AccountID { get; set; }

        public DateTime? LastRead { get; set; }

        public DateTime? LastPosted { get; set; }

        public virtual Account Account { get; set; }

        public virtual ForumThread ForumThread { get; set; }
    }
}
