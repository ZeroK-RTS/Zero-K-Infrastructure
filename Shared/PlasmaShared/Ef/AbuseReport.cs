namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("AbuseReport")]
    public partial class AbuseReport
    {
        public int AbuseReportID { get; set; }

        public int AccountID { get; set; }

        public int ReporterAccountID { get; set; }

        public DateTime Time { get; set; }

        [Required]
        public string Text { get; set; }

        public virtual Account AccountByAccountID { get; set; }

        public virtual Account AccountByReporterAccountID { get; set; }
    }
}
