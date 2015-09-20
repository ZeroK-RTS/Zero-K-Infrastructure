using System.Collections.Generic;
using System.Text;
using System.Web.Mvc;

namespace ZeroKWeb.ForumParser
{
    /// <summary>
    /// [q]some text[/q] - quoting someone
    /// </summary>
    public class QTagOpen : OpeningTag<QTagClose>
    {
        public override string Match { get; } = "[q]";


        public override LinkedListNode<Tag> Translate(StringBuilder sb, LinkedListNode<Tag> self, HtmlHelper html) {
            sb.Append(
                "<table border=\"0\" cellpadding=\"6\" cellspacing=\"0\" width=\"100%\"><tbody><tr><td style=\"border: 1px inset;\"><em>quote:<br/>");
            return self.Next;
        }

        public override Tag Create() {
            return new QTagOpen();
        }
    }

    /// <summary>
    /// [/q]
    /// </summary>
    public class QTagClose : ScanningTag
    {
        public override string Match { get; } = "[/q]";

        public override LinkedListNode<Tag> Translate(StringBuilder sb, LinkedListNode<Tag> self, HtmlHelper html)
        {
            sb.Append("</em></td></tr></tbody></table>");
            return self.Next;
        }

        public override Tag Create() {
            return new QTagClose();
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
