using System.ComponentModel.DataAnnotations;

namespace ZkData
{
    public partial class BlockedHost
    {
        [Key]
        public int HostID { get; set; }

        [Required]
        [StringLength(500)]
        public string HostName { get; set; }

        [StringLength(500)]
        public string Comment { get; set; }
    }
}
