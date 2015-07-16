using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LobbyClient;

namespace ZkData
{
    public class LobbyChatHistory
    {
        [Key]
        public int LobbyChatHistoryID { get; set; }
        public bool Ring { get; set; }

        public SayPlace SayPlace { get; set; }

        [MaxLength(255)]
        [Index]
        public string Target { get; set; }

        public string Text { get; set; }
        [Index(IsClustered = true)]
        public DateTime Time { get; set; }

        [MaxLength(255)]
        [Index]
        public string User { get; set; }

        public bool IsEmote { get; set; }

    }
}