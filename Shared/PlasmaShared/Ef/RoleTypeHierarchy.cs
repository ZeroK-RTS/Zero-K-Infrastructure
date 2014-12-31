namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("RoleTypeHierarchy")]
    public partial class RoleTypeHierarchy
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int MasterRoleTypeID { get; set; }

        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int SlaveRoleTypeID { get; set; }

        public bool CanAppoint { get; set; }

        public bool CanRecall { get; set; }

        public virtual RoleType RoleType { get; set; }

        public virtual RoleType RoleType1 { get; set; }
    }
}
