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


        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            context.Append(
                "<table border=\"0\" cellpadding=\"6\" cellspacing=\"0\" width=\"100%\"><tbody><tr><td style=\"border: 1px inset;\"><em>quote:<br/>");
            return self.Next;
        }

        public override Tag Create() => new QTagOpen();
    }

    /// <summary>
    /// [/q]
    /// </summary>
    public class QTagClose : ClosingTag
    {
        public override string Match { get; } = "[/q]";

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self)
        {
            context.Append("</em></td></tr></tbody></table>");
            return self.Next;
        }

        public override Tag Create() {
            return new QTagClose();
        }
    }


    /// <summary>
    /// [quote] alias for [q] tag
    /// </summary>
    public class QuoteTagOpen:OpeningTag<QuoteTagClose>
    {
        public override string Match { get; } = "[quote]";
        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            return new QTagOpen().Translate(context, self);
        }

        public override Tag Create() => new QuoteTagOpen();
    }

    /// <summary>
    /// [/quote] alias for [/q] tag
    /// </summary>
    public class QuoteTagClose : ClosingTag
    {
        public override string Match { get; } = "[/quote]";

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            return new QTagClose().Translate(context, self);
        }

        public override Tag Create() => new QuoteTagClose();
    }
}
