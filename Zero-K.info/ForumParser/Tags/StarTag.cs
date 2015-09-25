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

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {

            var node = self.Next;

            // to bolden text - find second star tag
            while (node != null && (node.Value is SpaceTag || node.Value is LiteralTag || node.Value is StarTag))
            {
                if (node.Value is StarTag)
                {
                    context.Append("<strong>");
                    self.Next.TranslateUntil(context, node); 
                    context.Append("</strong>");
                    return node.Next;
                }
                node = node.Next;
            }

            context.Append("*");
            return self.Next;
        }

        public override Tag Create() {
            return new StarTag();
        }
    }
}