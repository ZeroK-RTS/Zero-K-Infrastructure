using System.ComponentModel.DataAnnotations;

namespace ZkData
{
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
