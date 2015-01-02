namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("PollVote")]
    public partial class PollVote
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int AccountID { get; set; }

        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int PollID { get; set; }

        public int OptionID { get; set; }

        public virtual Account Account { get; set; }

        public virtual Poll Poll { get; set; }

        public virtual PollOption PollOption { get; set; }
    }
}
