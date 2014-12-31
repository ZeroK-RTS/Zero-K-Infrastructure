namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Avatar")]
    public partial class Avatar
    {
        [Key]
        [StringLength(50)]
        public string AvatarName { get; set; }
    }
}
