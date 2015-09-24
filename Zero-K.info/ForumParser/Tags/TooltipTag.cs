using System.Collections.Generic;
using System.Text;
using System.Web.Mvc;

namespace ZeroKWeb.ForumParser
{
	// example: [tooltip=Light Raider Bot]Glaive[/tooltip]
    public class TooltipOpenTag: OpeningArgsTag<TooltipCloseTag>
    {
        public override string Match { get; } = "[tooltip=";
        public override char MatchTerminator { get; } = ']';

        public override LinkedListNode<Tag> Translate(StringBuilder sb, LinkedListNode<Tag> self, HtmlHelper html) {
            sb.AppendFormat("<span nicetitle=\"{0}\">", args);
            return self.Next;
        }

        public override Tag Create() => new TooltipOpenTag();

        protected override bool ValidateArgs() {
            var str = args.ToString();
            return args.Length > 0 && !str.Contains("'") && !str.Contains("\"");
        }
    }

    public class TooltipCloseTag: ScanningTag
    {
        public override string Match { get; } = "[/tooltip]";

        public override LinkedListNode<Tag> Translate(StringBuilder sb, LinkedListNode<Tag> self, HtmlHelper html) {
            sb.Append("</span>");
            return self.Next;
        }

        public override Tag Create() => new TooltipCloseTag();
    }
}