using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Web.Mvc;

namespace ZeroKWeb.ForumParser
{
    public class ColorOpenTag: OpeningArgsTag<ColorCloseTag>
    {
        public override string Match { get; } = "[color=";
        public override char MatchTerminator { get; } = ']';

        public override LinkedListNode<Tag> Translate(StringBuilder sb, LinkedListNode<Tag> self, HtmlHelper html) {
            sb.AppendFormat("<font color=\"{0}\">", args);
            return self.Next;
        }

        protected override bool ValidateArgs() {
            if (args.Length == 0) return false;
            try
            {
                ColorTranslator.FromHtml(args.ToString());
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override Tag Create() => new ColorOpenTag();
    }

    public class ColorCloseTag: ScanningTag
    {
        public override string Match { get; } = "[/color]";

        public override LinkedListNode<Tag> Translate(StringBuilder sb, LinkedListNode<Tag> self, HtmlHelper html) {
            sb.Append("</font>");
            return self.Next;
        }

        public override Tag Create() => new ColorCloseTag();
    }
}