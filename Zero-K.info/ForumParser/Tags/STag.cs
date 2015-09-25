using System.Collections.Generic;
using System.Text;
using System.Web.Mvc;

namespace ZeroKWeb.ForumParser
{
    public class STagOpen : OpeningTag<STagClose>
    {
        public override string Match { get; } = "[s]";


        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self)
        {
            context.Append("<strike>");
            return self.Next;
        }

        public override Tag Create()
        {
            return new STagOpen();
        }
    }

    public class STagClose : ScanningTag
    {
        public override string Match { get; } = "[/s]";

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self)
        {
            context.Append("</strike>");
            return self.Next;
        }

        public override Tag Create()
        {
            return new STagClose();
        }
    }
}
