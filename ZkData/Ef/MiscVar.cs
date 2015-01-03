using System.ComponentModel.DataAnnotations;

namespace ZkData
{
    public partial class MiscVar
    {
        [Key]
        [StringLength(200)]
        public string VarName { get; set; }

        public string VarValue { get; set; }
    }
}
