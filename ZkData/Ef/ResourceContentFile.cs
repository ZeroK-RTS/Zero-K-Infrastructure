using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZkData
{
    public partial class ResourceContentFile
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ResourceID { get; set; }

        [Key]
        [Column(Order = 1)]
        [StringLength(32)]
        public string Md5 { get; set; }

        public int Length { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; }

        [Column(TypeName = "text")]
        public string Links { get; set; }

        public int LinkCount { get; set; }

        public virtual Resource Resource { get; set; }
    }
}
