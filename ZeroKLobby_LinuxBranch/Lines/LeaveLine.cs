using System;
using ZeroKLobby.MicroLobby;

namespace ZeroKLobby.Lines
{
    public class LeaveLine: IChatLine
    {
        public DateTime Date { get; set; }
        public string Reason { get; set; }
        public string UserName { get; set; }

        public LeaveLine(string userName, string reason)
        {
            Reason = reason;
            UserName = userName;
            Date = DateTime.Now;
            var icon = TextImage.GetUserImageCode(userName);
            Text = TextColor.Text + "[" + TextColor.Date + Date.ToShortTimeString() + TextColor.Text + "] " + icon + " " + TextColor.Leave + UserName +
                   TextColor.Text + " left" + " (" + Reason + ").";
        }

        public LeaveLine(string userName)
        {
            UserName = userName;
            Date = DateTime.Now;
            var icon = TextImage.GetUserImageCode(userName);
            Text = TextColor.Text + "[" + TextColor.Date + Date.ToShortTimeString() + TextColor.Text + "] " + icon + " " + TextColor.Leave + UserName +
                   TextColor.Text + " left.";
        }

        public string Text { get; private set; }
    }
}