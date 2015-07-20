using System;
using ZeroKLobby.MicroLobby;

namespace ZeroKLobby.Lines
{
    public class SaidExLine: IChatLine
    {
        public string AuthorName { get; set; }
        public DateTime Date { get; set; }
        public string Message { get; set; }


        public SaidExLine(string author, string message, DateTime? date = null)
        {
            AuthorName = author;
            Date = date ?? DateTime.Now;
            Message = message;
            var icon = TextImage.GetUserImageCode(author);
            Text = TextColor.Text + "[" + TextColor.Date + Date.ToLocalTime().ToShortTimeString() + TextColor.Text + "] " + icon + " " + TextColor.Emote + "* " +
                   AuthorName + " " + Message;
        }

        public string Text { get; private set; }
    }
}