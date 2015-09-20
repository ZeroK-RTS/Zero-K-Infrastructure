using System.Collections.Generic;
using System.Text;
using System.Web.Mvc;

namespace ZeroKWeb.ForumParser
{
    /// <summary>
    /// [url=http://something] content to show [/url]
    /// </summary>
    public class UrlOpenTag: OpeningTag<UrlCloseTag>
    {
        readonly StringBuilder args = new StringBuilder();
        public override string Match { get; } = "[url=";


        public override bool? ScanLetter(char letter) {
            if (pos >= Match.Length)
            {
                if (letter == ']')
                {
                    if (ForumWikiParser.IsValidLink(args.ToString())) return true;
                    return false;
                }
                if (letter == '\n') return false;
                args.Append(letter);
            } else if (char.ToLower(Match[pos++]) != char.ToLower(letter)) return false;

            return null;
        }

        public override LinkedListNode<Tag> Translate(StringBuilder sb, LinkedListNode<Tag> self, HtmlHelper html) {
            sb.AppendFormat("<a href=\"{0}\" target=\"_blank\">", args);
            return self.Next;
        }

        public override Tag Create() => new UrlOpenTag();
    }

    public class UrlCloseTag: ScanningTag
    {
        public override string Match { get; } = "[/url]";

        public override LinkedListNode<Tag> Translate(StringBuilder sb, LinkedListNode<Tag> self, HtmlHelper html) {
            sb.Append("</a>");
            return self.Next;
        }

        public override Tag Create() => new UrlCloseTag();
    }
}