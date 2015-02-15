using System;
using System.ComponentModel.DataAnnotations;

namespace ZkData
{
    public class AbuseReport
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
