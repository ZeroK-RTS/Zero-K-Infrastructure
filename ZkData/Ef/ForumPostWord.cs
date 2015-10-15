using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZkData
{
    public class ForumPostWord
    {
        [Key]
        [Column(Order = 0)]
        public int WordID { get; set; }
        [Key]
        [Column(Order = 1)]
        public int ForumPostID { get; set; }
        public int Count { get; set; } = 1;

        public virtual Word Word { get; set; }
        public virtual ForumPost ForumPost { get; set; }
    }
}