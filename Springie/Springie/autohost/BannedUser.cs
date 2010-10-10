#region using

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

#endregion

namespace Springie.autohost
{
    public class BannedUser
    {
        TimeSpan duration;

        [Description("Ban duration")]
        [XmlIgnore]
        public TimeSpan Duration { get { return duration; } set { duration = value; } }

        [XmlIgnore]
        public bool Expired { get { return (Started + Duration < DateTime.Now); } }

        public List<string> ipAddresses = new List<string>();

        [Description("Original name")]
        public string Name { get; set; }

        public List<string> nickNames = new List<string>();

        [Description("Reason of the ban")]
        public string Reason { get; set; }


        [Description("Ban start time")]
        public DateTime Started { get; set; }

        [Browsable(false)]
        public string XmlDuration { get { return duration.ToString(); } set { duration = TimeSpan.Parse(value); } }

        public BannedUser() {}

        public BannedUser(string name)
        {
            Name = name;
            Started = DateTime.Now;
            nickNames.Add(name);
        }
    }
}