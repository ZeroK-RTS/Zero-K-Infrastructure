using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZkData
{
    public class AccountUserID
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int AccountID { get; set; }
        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long UserID { get; set; }
        [Key]
        [Column(Order = 2)]
        [StringLength(50)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string InstallID { get; set; }

        public int LoginCount { get; set; }
        public DateTime FirstLogin { get; set; }
        public DateTime LastLogin { get; set; }

        public virtual Account Account { get; set; }
    }
}
