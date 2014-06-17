using System.Drawing;
using System.Text.RegularExpressions;
using ZeroKLobby;

namespace ZeroKLobby.MicroLobby
{
    public static class TextColor
    {
        public const int ircColorOffset = 16; //15 color reserved for mirc standard color
        public const int args = ircColorOffset + 2;
        public const int background = ircColorOffset + 0;
        public const char boldChar = (char)2; // not implemented
        public const char ColorChar = (char)3; //&#x3 or \x003
        public const char ColorResetChar = 'F';
        public const int date = ircColorOffset + 3;
        public const int emote = ircColorOffset + 4;
        public const int error = ircColorOffset + 5;
        public const int history = ircColorOffset + 6;
        public const int incomingCommand = ircColorOffset + 7;
        public const int join = ircColorOffset + 8;
        public const int leave = ircColorOffset + 9;
        public const int link = ircColorOffset + 10;
        public const int message = ircColorOffset + 11;
        public const int outgoingCommand = ircColorOffset + 12;
        public const char plainChar = (char)15;
        public const char reverseChar = (char)22;
        public const int text = ircColorOffset + 1;
        public const int topic = ircColorOffset + 14;
        public const int topicBackground = ircColorOffset + 15;
        public const char NewColorChar = '\xFF03';
        public const char UnderlineChar = (char)31;
        public const char UrlEnd = '\xFF0C';
        public const char UrlStart = '\xFF0B';
        public const char EmotChar = '\xFF0A';
        public const int username = ircColorOffset + 13;
        public static readonly string Args = ColorChar + args.ToString("00");
        public static readonly string Background = ColorChar + background.ToString("00");
        public static readonly string Date = ColorChar + date.ToString("00");
        public static readonly string Emote = ColorChar + emote.ToString("00");
        public static readonly string Error = ColorChar + error.ToString("00");
        public static readonly string History = ColorChar + history.ToString("00");
        public static readonly string IncomingCommand = ColorChar + incomingCommand.ToString("00");
        public static readonly string Join = ColorChar + join.ToString("00");
        public static readonly string Leave = ColorChar + leave.ToString("00");
        public static readonly string Link = ColorChar + link.ToString("00");
        public static readonly string Message = ColorChar + message.ToString("00");
        public static readonly string OutgoingCommand = ColorChar + outgoingCommand.ToString("00");
        public static readonly string Text = ColorChar + text.ToString("00");
        public static readonly string Topic = ColorChar + topic.ToString("00");
        public static readonly string TopicBackground = ColorChar + topicBackground.ToString("00");
        public static readonly string Username = ColorChar + username.ToString("00");
        static Regex allCodesExceptEmot = new Regex("\xFF03[0-9]{4}|\xFF0B|\xFF0C|\x3\\d{2},\\d{2}|\x3\\d{2}|\x3|\xF");
        static Regex allCodes = new Regex("\xFF03[0-9]{4}|\xFF0B|\xFF0C|\x3\\d{2},\\d{2}|\x3\\d{2}|\x3|\xF|\xFF0A\\d{3}|\xFF0A");
        static Regex codes = new Regex("\xFF03[0-9]{4}|\x3\\d{2},\\d{2}|\x3\\d{2}|\x3|\xF");
        static Color[] colors;
        public const int colorRange = 16 + ircColorOffset;


        static TextColor()
        {
            colors = new Color[colorRange]; //be sure to increment this when adding colors
            colors[0] = Color.FromArgb(255, 255, 255); //white, see https://github.com/myano/jenni/wiki/IRC-String-Formatting and http://www.w3schools.com/html/html_colornames.asp
            colors[1] = Color.FromArgb(0, 0, 0); //black
            colors[2] = Color.FromArgb(0, 0, 255); //blue
            colors[3] = Color.FromArgb(0, 128, 0); //green
            colors[4] = Color.FromArgb(255, 0, 0); //red
            colors[5] = Color.FromArgb(165, 42, 42); //brown
            colors[6] = Color.FromArgb(128, 0, 128); //purple
            colors[7] = Color.FromArgb(255, 165, 0); //orange
            colors[8] = Color.FromArgb(255, 255, 0); //yellow
            colors[9] = Color.FromArgb(144, 238, 144); //light green
            colors[10] = Color.FromArgb(0, 128, 128); //teal
            colors[11] = Color.FromArgb(224, 255, 255); //light cyan
            colors[12] = Color.FromArgb(173, 216, 230); //light blue 
            colors[13] = Color.FromArgb(255, 192, 203); //pink 
            colors[14] = Color.FromArgb(128, 128, 128); //grey
            colors[15] = Color.FromArgb(211, 211, 211); //light grey
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
            return colorCode >= colors.Length ? colors[text] : colors[colorCode]; //color code or use text color as defaut
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

        public static string StripAllCodesExceptEmot(this string line) //Strip all codes except emot
        {
            if (line == null) return string.Empty;

            if (line.Length > 0)
            {
                var strippedLine = allCodesExceptEmot.Replace(line, "");
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