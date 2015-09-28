using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Web.Mvc;

namespace ZeroKWeb.ForumParser
{
    // [color=red][/color]
    public class ColorOpenTag: OpeningArgsTag<ColorCloseTag>
    {
        public override string Match { get; } = "[color=";
        public override char MatchTerminator { get; } = ']';

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            context.AppendFormat("<font color=\"{0}\">", arguments);
            return self.Next;
        }

        protected override bool ValidateArgs(string args) {
            if (args.Length == 0) return false;
            try
            {
                ColorTranslator.FromHtml(args);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override Tag Create() => new ColorOpenTag();
    }

    public class ColorCloseTag: ClosingTag
    {
        public override string Match { get; } = "[/color]";

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            context.Append("</font>");
            return self.Next;
        }

        public override Tag Create() => new ColorCloseTag();
    }
}