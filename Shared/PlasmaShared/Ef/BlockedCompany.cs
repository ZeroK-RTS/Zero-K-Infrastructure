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
        public string CompanyName { get; set; }

        public string Comment { get; set; }
    }
}
