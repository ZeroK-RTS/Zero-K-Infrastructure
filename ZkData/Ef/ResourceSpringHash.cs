using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZkData
{
    public class ResourceSpringHash
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ResourceID { get; set; }
        [Key]
        [Column(Order = 1)]
        [StringLength(50)]
        public string SpringVersion { get; set; }
        public int SpringHash { get; set; }

        public virtual Resource Resource { get; set; }
    }
}
