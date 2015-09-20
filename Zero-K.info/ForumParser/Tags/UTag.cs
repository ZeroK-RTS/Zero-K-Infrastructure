using System.Collections.Generic;
using System.Text;
using System.Web.Mvc;

namespace ZeroKWeb.ForumParser
{
    public class UTagOpen: OpeningTag<UTagClose>
    {
        public override string Match { get; } = "[u]";


        public override LinkedListNode<Tag> Translate(StringBuilder sb, LinkedListNode<Tag> self, HtmlHelper html) {
            sb.Append("<u>");
            return self.Next;
        }

        public override Tag Create() {
            return new UTagOpen();
        }
    }

    public class UTagClose: ScanningTag
    {
        public override string Match { get; } = "[/u]";

        public override LinkedListNode<Tag> Translate(StringBuilder sb, LinkedListNode<Tag> self, HtmlHelper html) {
            sb.Append("</u>");
            return self.Next;
        }

        public override Tag Create() {
            return new UTagClose();
        }
    }
}

