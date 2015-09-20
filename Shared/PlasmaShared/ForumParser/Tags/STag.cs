using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlasmaShared.ForumParser
{
    public class STagOpen : OpeningTag<STagClose>
    {
        public override string Match { get; } = "[s]";


        public override LinkedListNode<Tag> Translate(StringBuilder sb, LinkedListNode<Tag> self)
        {
            sb.Append("<strike>");
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

        public override LinkedListNode<Tag> Translate(StringBuilder sb, LinkedListNode<Tag> self)
        {
            sb.Append("</strike>");
            return self.Next;
        }

        public override Tag Create()
        {
            return new STagClose();
        }
    }
}
