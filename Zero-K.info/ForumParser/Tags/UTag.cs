using System.Collections.Generic;
using System.Text;
using System.Web.Mvc;

namespace ZeroKWeb.ForumParser
{
    public class UTagOpen: OpeningTag<UTagClose>
    {
        public override string Match { get; } = "[u]";


        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            context.Append("<u>");
            return self.Next;
        }

        public override Tag Create() {
            return new UTagOpen();
        }
    }

    public class UTagClose: ClosingTag
    {
        public override string Match { get; } = "[/u]";

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            context.Append("</u>");
            return self.Next;
        }

        public override Tag Create() {
            return new UTagClose();
        }
    }
}

