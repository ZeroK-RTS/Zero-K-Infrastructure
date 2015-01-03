using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZkData
{
    public partial class ResourceDependency
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ResourceID { get; set; }

        [Key]
        [Column(Order = 1)]
        [StringLength(250)]
        public string NeedsInternalName { get; set; }

        public virtual Resource Resource { get; set; }
    }
}
