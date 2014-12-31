namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("BlockedHost")]
    public partial class BlockedHost
    {
        [Key]
        public int HostID { get; set; }

        [Required]
        public string HostName { get; set; }

        public string Comment { get; set; }
    }
}
