using System;
using JetBrains.Annotations;
using ZeroKLobby.MicroLobby;

namespace ZeroKLobby.Lines
{
    public class SaidLine: IChatLine
    {
        public string AuthorName { get; set; }
        public DateTime Date { get; set; }
        public string Message { get; set; }

        public SaidLine(string author, [NotNull] string message, DateTime? date = null)
        {
            if (message == null) throw new ArgumentNullException("message");
            var myName = Program.Conf.LobbyPlayerName;
            var me = myName == author;
        		var hilite = message.Contains(myName) && !message.StartsWith(string.Format("[{0}]", myName));

            AuthorName = author;
						if (date != null) Date = date.Value;
						else Date = DateTime.Now;
            Message = message;
            var icon = TextImage.GetUserImageCode(author);

            var textColor = TextColor.Text;
            if (me) textColor = TextColor.History;
            else if (hilite) textColor = TextColor.Error;

            Text = TextColor.Text + "[" + TextColor.Date + Date.ToShortTimeString() + TextColor.Text + "] " + icon + " " + TextColor.Username +
                   AuthorName + textColor + " " + Message;
        }


        public string Text { get; private set; }
    }
}