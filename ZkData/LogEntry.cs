using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace ZkData
{
    public class LogEntry
    {
        [Key]
        public int LogEntryID { get; set; }
        public TraceEventType TraceEventType { get; set; }
        public string Message { get; set; }
        public DateTime Time { get; set; }
    }
}