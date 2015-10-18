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

            context.Append(Text);
            return self.Next;

        }

        public override bool? AcceptsLetter(ParseContext context, char letter) {
            if (letter == '_')
            {
                var lastLit = (context.PreviousTag?.Value as LiteralTag);
                if (lastLit?.Text.IsValidLinkOrRelativeUrl() == true) return false;
            }
            return base.AcceptsLetter(context, letter);
        }

        public override Tag Create() => new UnderscoreTag();

        public override string Match { get; } = "_";
    }
}