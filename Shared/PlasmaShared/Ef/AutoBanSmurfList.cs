namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("AutoBanSmurfList")]
    public partial class AutoBanSmurfList
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int AccountID { get; set; }

        public bool BanLobby { get; set; }

        public bool BanSite { get; set; }

        public bool BanIP { get; set; }

        public bool BanUserID { get; set; }

        public virtual Account Account { get; set; }
    }
}
