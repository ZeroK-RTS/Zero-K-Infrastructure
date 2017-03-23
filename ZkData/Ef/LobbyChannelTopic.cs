using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ZkData
{
    public class LobbyChannelTopic
    {
        [Key]
        [MaxLength(200)]
        public string ChannelName { get; set; }

        public string Topic { get; set; }

        public string Author { get; set; }

        public DateTime SetDate { get; set; }
    }
}