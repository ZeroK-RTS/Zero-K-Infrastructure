using System.Collections.Generic;
using System.Text;
using System.Web.Mvc;

namespace ZeroKWeb.ForumParser
{
    public class SizeOpenTag: OpeningArgsTag<SizeCloseTag>
    {
        public override string Match { get; } = "[size=";
        public override char MatchTerminator { get; } = ']';

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            context.AppendFormat("<font size=\"{0}\">", arguments);
            return self.Next;
        }

        public override Tag Create() => new SizeOpenTag();

        protected override bool ValidateArgs(ParseContext context, string args) {
            return args.Length > 0 && !args.Contains("'") && !args.Contains("\"");
        }
    }

    public class SizeCloseTag: ClosingTag
    {
        public override string Match { get; } = "[/size]";

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            context.Append("</font>");
            return self.Next;
        }

        public override Tag Create() => new SizeCloseTag();
    }
}