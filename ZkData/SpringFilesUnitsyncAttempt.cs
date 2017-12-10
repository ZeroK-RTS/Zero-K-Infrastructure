using System;
using System.ComponentModel.DataAnnotations;

namespace ZkData {
    public class SpringFilesUnitsyncAttempt
    {
        [Key]
        public int SpringFilesUnitsyncAttemptID { get; set; }
        public string FileName { get; set; }
        public DateTime Time { get; set; } = DateTime.Now;

    }
}