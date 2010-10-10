using SpringDownloader.MicroLobby;

namespace SpringDownloader.Lines
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