using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZkData
{
    public partial class ForumLastRead
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int AccountID { get; set; }

        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ForumCategoryID { get; set; }

        public DateTime? LastRead { get; set; }

        public virtual Account Account { get; set; }

        public virtual ForumCategory ForumCategory { get; set; }
    }
}
