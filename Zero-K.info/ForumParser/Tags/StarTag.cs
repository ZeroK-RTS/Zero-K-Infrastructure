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
            while (node != null && (node.Value is SpaceTag || node.Value is LiteralTag || node.Value is StarTag))
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
                var endline = n.FirstNode(x => x.Value is NewLineTag);
                var p = ListPrefixLevel(n);
                if (p == level) // same level add item
                {
                    context.Append("<li>\n");
                    n.Next?.Next?.TranslateUntilNode(context, endline);
                    context.Append("</li>\n");
                } else if (p > level) // increasing level, start list, recursion
                {
                    context.Append("<ul>\n");
                    endline = ProcessListContent(context, p, n);
                    context.Append("</ul>\n");
                } else // decreasing level
                    return n.Previous?.Previous; // this is continuation of lower level ul, return endline so taht code can resume

                n = endline?.Next;

                if (n != null) // advance to next line - if possible
                {
                    var nextLineStar = n.FirstNode(x => (x.Value is StarTag || x.Value is NewLineTag));
                    if (nextLineStar?.Value is StarTag)
                    {
                        var nextPrefix = ListPrefixLevel(nextLineStar);
                        if (nextPrefix > 0) n = nextLineStar; // move to star on next line
                        else break; // stop ul list - invalid prefix 
                    } else break; // stop ul, not starred list at all
                }
            } while (n != null);
            return n;
        }

        public override Tag Create() {
            return new StarTag();
        }
    }
}