using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlasmaShared.ForumParser
{
    /// <summary>
    /// [q]some text[/q] - quoting someone
    /// </summary>
    public class QTagOpen : OpeningTag<QTagClose>
    {
        public override string Match { get; } = "[q]";

        public override void RenderSelf(StringBuilder sb)
        {
            sb.Append(
                "<table border=\"0\" cellpadding=\"6\" cellspacing=\"0\" width=\"100%\"><tbody><tr><td style=\"border: 1px inset;\"><em>quote:<br/>");
        }
    }

    /// <summary>
    /// [/q]
    /// </summary>
    public class QTagClose : ScanningTag
    {
        public override string Match { get; } = "[/q]";

        public override LinkedListNode<Tag> Translate(StringBuilder sb, LinkedListNode<Tag> self)
        {
            sb.Append("</em></td></tr></tbody></table>");
            return self.Next;
        }
    }


    /// <summary>
    /// [quote] alias for [q] tag
    /// </summary>
    public class QuoteTagOpen : QTagOpen
    {
        public override string Match { get; } = "[quote]";
    }

    /// <summary>
    /// [/quote] alias for [/q] tag
    /// </summary>
    public class QuoteTagClose : QTagClose
    {
        public override string Match { get; } = "[/quote]";
    }
}
