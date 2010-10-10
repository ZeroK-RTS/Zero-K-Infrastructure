using System.Drawing;
using System.Text.RegularExpressions;
using ZeroKLobby;

namespace ZeroKLobby.MicroLobby
{
    public static class TextColor
    {
        const int args = 2;
        public static readonly string Args = ColorChar + args.ToString("00");
        const int background = 0;
        public static readonly string Background = ColorChar + background.ToString("00");
        const char boldChar = (char)2; // not implemented
        public const char ColorChar = (char)3;
        const int date = 3;
        public static readonly string Date = ColorChar + date.ToString("00");
        public const char EmotChar = '\xFF0A';
        const int emote = 4;
        public static readonly string Emote = ColorChar + emote.ToString("00");
        const int error = 5;
        public static readonly string Error = ColorChar + error.ToString("00");
        const int history = 6;
        public static readonly string History = ColorChar + history.ToString("00");
        const int incomingCommand = 7;
        public static readonly string IncomingCommand = ColorChar + incomingCommand.ToString("00");
        const int join = 8;
        public static readonly string Join = ColorChar + join.ToString("00");
        const int leave = 9;
        public static readonly string Leave = ColorChar + leave.ToString("00");
        const int link = 10;
        public static readonly string Link = ColorChar + link.ToString("00");
        const int message = 11;
        public static readonly string Message = ColorChar + message.ToString("00");
        public const char NewColorChar = '\xFF03';
        const int outgoingCommand = 12;
        public static readonly string OutgoingCommand = ColorChar + outgoingCommand.ToString("00");
        const char plainChar = (char)15;
        const char reverseChar = (char)22;
        const int text = 1;
        public static readonly string Text = ColorChar + text.ToString("00");
        const int topic = 14;
        public static readonly string Topic = ColorChar + topic.ToString("00");
        const int topicBackground = 15;
        public static readonly string TopicBackground = ColorChar + topicBackground.ToString("00");
        public const char UnderlineChar = (char)31;
        public const char UrlEnd = '\xFF0C';
        public const char UrlStart = '\xFF0B';
        const int username = 13;
        public static readonly string Username = ColorChar + username.ToString("00");

        static Regex allCodes = new Regex("\xFF03[0-9]{4}|\xFF0B|\xFF0C|\x3\\d{2},\\d{2}|\x3\\d{2}|\xFF0A\\d{3}|\xFF0A");
        static Regex codes = new Regex("\xFF03[0-9]{4}|\x3\\d{2},\\d{2}|\x3\\d{2}");
        static Color[] colors;


        static TextColor()
        {
            colors = new Color[16]; //be to increment this when adding colors

            colors[background] = Program.Conf.BgColor;
            colors[text] = Program.Conf.TextColor;
            colors[args] = Program.Conf.TextColor;
            colors[date] = Program.Conf.TextColor;
            colors[emote] = Program.Conf.EmoteColor;
            colors[error] = Program.Conf.NoticeColor;
            colors[history] = Program.Conf.FadeColor;
            colors[incomingCommand] = Color.FromArgb(100, 50, 255); // for debugging
            colors[join] = Program.Conf.JoinColor;
            colors[leave] = Program.Conf.LeaveColor;
            colors[link] = Program.Conf.LinkColor;
            colors[message] = Program.Conf.NoticeColor;
            colors[outgoingCommand] = Color.FromArgb(0, 200, 0); // for debugging
            colors[username] = Program.Conf.LinkColor;
            colors[topic] = Color.Black;
            colors[topicBackground] = Color.FromArgb(255, 255, 225);
        }

        public static Color GetColor(int colorCode)
        {
            return colorCode >= colors.Length ? colors[text] : colors[colorCode];
        }

        public static string StripAllCodes(this string line)
        {
            if (line == null) return string.Empty;

            if (line.Length > 0)
            {
                var strippedLine = allCodes.Replace(line, ""); // does not replace all tags
                return strippedLine;
            }
            return string.Empty;
        }

        public static string StripCodes(string line)
        {
            return codes.Replace(line, "");
        }
    }
}