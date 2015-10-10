using System.Collections.Generic;
using System.Linq;

namespace ZeroKWeb.ForumParser
{
    /// <summary>
    ///     construct tables using || something ||
    /// </summary>
    public class TableCellTag: ScanningTag
    {
        public override string Match { get; } = "||";

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            var cellCount = self.AsEnumerable().Skip(1).TakeWhile(x => !(x.Value is NewLineTag)).Count(x => x.Value is TableCellTag);

            // only start tables if this tag appears on start of line
            if (cellCount > 0 && self.AsReverseEnumerable().TakeWhile(x => !(x.Value is NewLineTag)).All(x => x.Value is SpaceTag)) 
            {
                context.Append("<table class='wikitable'>");

                var n = self;
                while (n?.Value is TableCellTag)
                {
                    if (cellCount != n.AsEnumerable().Skip(1).TakeWhile(x => !(x.Value is NewLineTag)).Count(x => x.Value is TableCellTag)) break;

                    context.Append("<tr>");

                    LinkedListNode<Tag> nextStop;
                    while ((nextStop = n.Next.FirstNode(x => x.Value is NewLineTag || x.Value is TableCellTag))?.Value is TableCellTag)
                    {
                        context.Append("<td>");
                        n.Next.TranslateUntilNode(context, nextStop);
                        context.Append("</td>");
                        n = nextStop;
                    }

                    context.Append("</tr>");

                    n = n.FirstNode(x => x.Value is NewLineTag)?.Next; // move after nextline
                }
                context.Append("</table>");
                return n;
            }
            context.Append(Text);
            
            return self.Next;
        }

        public override Tag Create() => new TableCellTag();
    }
}