using System;
using ZeroKLobby.MicroLobby;

namespace ZeroKLobby.Lines
{
    public class JoinLine: IChatLine
    {
        public DateTime Date { get; set; }
        public string UserName { get; set; }

        public JoinLine(string userName)
        {
            UserName = userName;
            Date = DateTime.Now;
            var icon = TextImage.GetUserImageCode(userName);
            Text = TextColor.Text + "[" + TextColor.Date + Date.ToShortTimeString() + TextColor.Text + "] " + icon + " " + TextColor.Join + UserName +
                   TextColor.Text + " joined.";
        }

        public string Text { get; private set; }
    }
}