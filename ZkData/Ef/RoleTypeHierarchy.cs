using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZkData
{
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

        public virtual RoleType MasterRoleType { get; set; }

        public virtual RoleType SlaveRoleType { get; set; }
    }
}
