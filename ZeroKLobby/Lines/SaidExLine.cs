using System;
using SpringDownloader.MicroLobby;

namespace SpringDownloader.Lines
{
    public class SaidExLine: IChatLine
    {
        public string AuthorName { get; set; }
        public DateTime Date { get; set; }
        public string Message { get; set; }


        public SaidExLine(string author, string message)
        {
            AuthorName = author;
            Date = DateTime.Now;
            Message = message;
            var icon = TextImage.GetUserImageCode(author);
            Text = TextColor.Text + "[" + TextColor.Date + Date.ToShortTimeString() + TextColor.Text + "] " + icon + " " + TextColor.Emote + "* " +
                   AuthorName + " " + Message;
        }

        public string Text { get; private set; }
    }
}