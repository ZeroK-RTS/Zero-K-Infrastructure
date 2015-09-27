using System.Collections.Generic;
using System.Linq;

namespace ZeroKWeb.ForumParser
{
    /// <summary>
    ///  construct tables using || something || 
    /// </summary>
    public class TableCellTag: ScanningTag
    {
        public override string Match { get; } = "||";

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            var cellCount = self.AsEnumerable().Skip(1).TakeWhile(x => !(x.Value is NewLineTag)).Count(x => x.Value is TableCellTag);

            if (cellCount > 0)
            {
                context.Append("<table>");

                var n = self;
                do
                {
                    if (n.Value is TableCellTag)
                    {
                        var cnt = n.AsEnumerable().Skip(1).TakeWhile(x => !(x.Value is NewLineTag)).Count(x => x.Value is TableCellTag);
                        if (cnt == cellCount)
                        {
                            context.Append("<tr>");
                            do
                            {
                                context.Append("<td>");
                                n = n.Next.TranslateWhile(context, x => !(x.Value is NewLineTag || x.Value is TableCellTag));
                                context.Append("</td>");
                            } while (n?.Value is TableCellTag);
                            context.Append("</tr>");
                            n = n.FirstNode(x => x.Value is NewLineTag)?.Next; // move after nextline
                        } else break;
                    } else break;

                    n = n.FirstNode(x => x.Value is NewLineTag)?.Next;
                } while (n != null);

                context.Append("</table>");
                return n;

            } else context.Append(GetOriginalContent());

            return self.Next;
        }

        public override Tag Create() => new TableCellTag();
    }
}