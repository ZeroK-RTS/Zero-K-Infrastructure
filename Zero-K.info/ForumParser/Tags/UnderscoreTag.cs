using System.Collections.Generic;

namespace ZeroKWeb.ForumParser
{
    public class UnderscoreTag: ScanningTag
    {
        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {

            // to bolden text - find second star tag
            var node = self.Next;
            while (node != null && (node.Value is SpaceTag || node.Value is LiteralTag || node.Value is StarTag || node.Value is UnderscoreTag))
            {
                if (node.Value is UnderscoreTag)
                {
                    context.Append("<em>");
                    self.Next.TranslateUntilNode(context, node);
                    context.Append("</em>");
                    return node.Next;
                }
                node = node.Next;
            }

            context.Append(GetOriginalContent());
            return self.Next;

        }

        public override Tag Create() => new UnderscoreTag();

        public override string Match { get; } = "_";
    }
}