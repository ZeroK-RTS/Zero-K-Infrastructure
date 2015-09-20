using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Mvc;

namespace ZeroKWeb.ForumParser
{
    /// <summary>
    /// [img]http://url[/img]  or [img=http://url][/img]
    /// </summary>
    public class ImgOpenTag: OpeningTag<ImgCloseTag>
    {
        readonly StringBuilder args = new StringBuilder();
        public override string Match { get; } = "[img";


        public override bool? ScanLetter(char letter) {
            if (pos >= Match.Length)
            {
                if (letter == ']')
                {
                    if (args.Length == 0) return true; // closed [img]
                    if (ForumWikiParser.IsValidLink(args.ToString(1, args.Length - 1))) return true; //  [img=url]
                    return false;
                }
                if (letter == '\n') return false;
                args.Append(letter);
            } else if (char.ToLower(Match[pos++]) != char.ToLower(letter)) return false;

            return null;
        }

        public override LinkedListNode<Tag> Translate(StringBuilder sb, LinkedListNode<Tag> self, HtmlHelper html) {
            var closingTag = ForumWikiParser.NextNodeOfType<ImgCloseTag>(self);

            // get url either from param or from inner literal between tags
            var url = args.Length == 0 ? (self.Next?.Value as LiteralTag)?.Content.ToString() : args.ToString(1, args.Length - 1);

            if (ForumWikiParser.IsValidLink(url)) sb.AppendFormat("<a href=\"{0}\" target=\"_blank\" ><img src=\"{0}\" max-width=\"100%\" height=\"auto\"/></a>", url);

            return closingTag?.Next; // move to after closing img
        }


        public override Tag Create() => new ImgOpenTag();
    }


    public class ImgCloseTag: ScanningTag
    {
        public override string Match { get; } = "[/img]";

        public override LinkedListNode<Tag> Translate(StringBuilder sb, LinkedListNode<Tag> self, HtmlHelper html) {
            throw new NotImplementedException(); // this should never be actually executed, opening tag handles it
        }

        public override Tag Create() => new ImgCloseTag();
    }
}