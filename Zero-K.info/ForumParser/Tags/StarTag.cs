using System.Collections.Generic;
using System.Text;
using System.Web.Mvc;

namespace ZeroKWeb.ForumParser
{
    /// <summary>
    /// *literal* bold text alternative wiki
    /// </summary>
    public class StarTag: ScanningTag
    {
        public override string Match { get; } = "*";

        public override LinkedListNode<Tag> Translate(StringBuilder sb, LinkedListNode<Tag> self, HtmlHelper html) {
            if (self.Next?.Value is LiteralTag && self.Next?.Next?.Value is StarTag)
            {
                sb.Append("<strong>");
                self.Next.Value.Translate(sb, self.Next, html);
                sb.Append("</strong>");
                return self.Next.Next.Next;
            } else
            {
                sb.Append("*");
                return self.Next;
            }
        }

        public override Tag Create() {
            return new StarTag();
        }
    }
}