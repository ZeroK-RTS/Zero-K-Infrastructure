using System.Collections.Generic;
using System.Text;
using System.Web.Mvc;

namespace ZeroKWeb.ForumParser
{
    /// <summary>
    ///     [url=http://something] content to show [/url]
    /// </summary>
    public class UrlOpenTag: OpeningArgsTag<UrlCloseTag>
    {
        public override string Match { get; } = "[url=";
        public override char MatchTerminator { get; } = ']';

        protected override bool ValidateArgs() => args.ToString().IsValidLink();

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            context.AppendFormat("<a href=\"{0}\" target=\"_blank\">", args);
            return self.Next;
        }

        public override Tag Create() => new UrlOpenTag();
    }

    public class UrlCloseTag: ClosingTag
    {
        public override string Match { get; } = "[/url]";

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            context.Append("</a>");
            return self.Next;
        }

        public override Tag Create() => new UrlCloseTag();
    }
}