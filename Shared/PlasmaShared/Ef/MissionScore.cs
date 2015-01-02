namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("MissionScore")]
    public partial class MissionScore
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int MissionID { get; set; }

        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int AccountID { get; set; }

        public int Score { get; set; }

        public DateTime Time { get; set; }

        public int MissionRevision { get; set; }

        public int GameSeconds { get; set; }

        public virtual Account Account { get; set; }

        public virtual Mission Mission { get; set; }
    }
}
