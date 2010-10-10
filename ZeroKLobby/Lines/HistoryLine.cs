using ZeroKLobby.MicroLobby;

namespace ZeroKLobby.Lines
{
    class HistoryLine: IChatLine
    {
        public HistoryLine(string text)
        {
            Text = TextColor.History + text.StripAllCodes();
        }

        public string Text { get; private set; }
    }
}