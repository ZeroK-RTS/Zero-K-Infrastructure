using System.ComponentModel.DataAnnotations;

namespace ZkData
{
    public partial class Avatar
    {
        [Key]
        [StringLength(50)]
        public string AvatarName { get; set; }
    }
}
