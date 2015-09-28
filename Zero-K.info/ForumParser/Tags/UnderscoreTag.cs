using System.Collections.Generic;

namespace ZeroKWeb.ForumParser
{
    public class UnderscoreTag: ScanningTag
    {
        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {

            // to bolden text - find second star tag
            var node = self.Next;
            while (node != null && !(node.Value is NewLineTag) && node.Value.Mode == OpeningClosingMode.SelfClosed && !(node.Value is TableCellTag))
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

        public override bool? ScanLetter(ParseContext context, char letter) {
            if (letter == '_')
            {
                var lastLit = (context.PreviousTag?.Value as LiteralTag)?.GetOriginalContent();
                if (lastLit.IsValidLink()) return false;
            }
            return base.ScanLetter(context, letter);
        }

        public override Tag Create() => new UnderscoreTag();

        public override string Match { get; } = "_";
    }
}