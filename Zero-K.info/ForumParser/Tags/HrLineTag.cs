using System.Collections.Generic;

namespace ZeroKWeb.ForumParser
{
    /// <summary>
    /// ---- for hr tag
    /// </summary>
    public class HrLineTag:ScanningTag
    {
        public override bool? ScanLetter(ParseContext context, char letter) {
            var ret =base.ScanLetter(context, letter);
            if (ret == true && !(context.NextChar ==null|| context.NextChar =='\r' || context.NextChar=='\n')) return false;
            return ret;
        }


        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            context.Append("<hr/>");
            return self.Next;
        }

        public override Tag Create() => new HrLineTag();

        public override string Match { get; } = "----";
    }
}