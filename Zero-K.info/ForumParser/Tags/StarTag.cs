using System.Collections.Generic;

namespace ZeroKWeb.ForumParser
{
    /// <summary>
    ///     *literal* bold text alternative wiki
    /// </summary>
    public class StarTag: ScanningTag
    {
        public override string Match { get; } = "*";


        static int ListPrefixLevel(LinkedListNode<Tag> star) {
            var el = star?.Previous?.Previous?.Value;
            var prefix = star?.Previous?.Value as SpaceTag;
            if ((el == null || el is NewLineTag) && prefix != null && star.Next?.Value is SpaceTag) return prefix.GetOriginalContent().Length;
            return 0;
        }

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            // unordered lists
            if (ListPrefixLevel(self) > 0) return ProcessListContent(context, 0, self);


            // to bolden text - find second star tag
            var node = self.Next;
            while (node != null && !(node.Value is NewLineTag) && node.Value.Mode == OpeningClosingMode.SelfClosed && !(node.Value is TableCellTag))
            {
                if (node.Value is StarTag)
                {
                    context.Append("<strong>");
                    self.Next.TranslateUntilNode(context, node);
                    context.Append("</strong>");
                    return node.Next;
                }
                node = node.Next;
            }

            context.Append("*");
            return self.Next;
        }

        static LinkedListNode<Tag> ProcessListContent(TranslateContext context, int level, LinkedListNode<Tag> start) {
            var n = start;
            do
            {
                var thisLineStar = n.FirstNode(x => (x.Value is StarTag || x.Value is NewLineTag));
                if (thisLineStar == null || thisLineStar.Value is NewLineTag || ListPrefixLevel(thisLineStar) == 0) return n;
                n = thisLineStar;

                var endline = n.FirstNode(x => x.Value is NewLineTag);
                var p = ListPrefixLevel(n);
                if (p == level) // same level add item
                {
                    context.Append("<li>\n");
                    n.Next?.Next?.TranslateUntilNode(context, endline);
                    context.Append("</li>\n");
                    n = endline?.Next;
                } else if (p > level) // increasing level, start list, recursion
                {
                    context.Append("<ul>\n");
                    n = ProcessListContent(context, p, n);
                    context.Append("</ul>\n");
                } else // decreasing level
                    return n; // this is continuation of lower level ul, return endline so taht code can resume

            } while (n != null);
            return n;
        }

        public override Tag Create() {
            return new StarTag();
        }
    }
}