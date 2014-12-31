namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("BlockedCompany")]
    public partial class BlockedCompany
    {
        [Key]
        public int CompanyID { get; set; }

        [Required]
        [StringLength(500)]
        public string CompanyName { get; set; }

        [StringLength(500)]
        public string Comment { get; set; }
    }
}
