namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("ExceptionLog")]
    public partial class ExceptionLog
    {
        public int ExceptionLogID { get; set; }

        public int ProgramID { get; set; }

        [Required]
        public string Exception { get; set; }

        public string ExtraData { get; set; }

        [StringLength(50)]
        public string RemoteIP { get; set; }

        [StringLength(200)]
        public string PlayerName { get; set; }

        public DateTime Time { get; set; }

        [StringLength(100)]
        public string ProgramVersion { get; set; }

        [Required]
        [StringLength(32)]
        public string ExceptionHash { get; set; }
    }
}
