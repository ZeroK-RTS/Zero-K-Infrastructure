using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZkData
{
    public partial class CommanderDecorationIcon
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int DecorationUnlockID { get; set; }

        public int IconPosition { get; set; }

        public int IconType { get; set; }

        public virtual Unlock Unlock { get; set; }
    }
}
