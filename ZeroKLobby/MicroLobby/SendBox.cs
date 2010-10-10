using System;
using System.Windows.Forms;
using PlasmaShared;

namespace SpringDownloader.MicroLobby
{
    public class SendBox: TextBox
    {
        public event EventHandler<EventArgs<string>> LineEntered = delegate { };

        public SendBox()
        {
            Multiline = true;
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (Lines.Length > 1)
            {
                var line = Text.Replace("\t","  ").TrimEnd(new[] { '\r', '\n' });
                Text = String.Empty;
                LineEntered(this, new EventArgs<string>(line));
            }
            base.OnKeyUp(e);
        }
    }
}