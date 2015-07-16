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


        public Say ToSay()
        {
            return new Say() { IsEmote = IsEmote, Place = SayPlace, User = User, Ring = Ring, Target = Target, Text = Text, Time = Time };
        }

        public void SetFromSay(Say say)
        {
            this.IsEmote = say.IsEmote;
            this.SayPlace = say.Place;
            this.User = say.User;
            this.Ring = say.Ring;
            this.Target = say.Target;
            this.Text = say.Text;
            this.Time = say.Time ?? DateTime.UtcNow;
        }

    }
}