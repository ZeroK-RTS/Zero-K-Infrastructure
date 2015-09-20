using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlasmaShared.ForumParser
{
    public class ITagOpen: OpeningTag<ITagClose>
    {
        public override string Match { get; } = "[i]";


        public override LinkedListNode<Tag> Translate(StringBuilder sb, LinkedListNode<Tag> self) {
            sb.Append("<em>");
            return self.Next;
        }

        public override Tag Create() {
            return new ITagOpen();
        }
    }

    public class ITagClose: ScanningTag
    {
        public override string Match { get; } = "[/i]";

        public override LinkedListNode<Tag> Translate(StringBuilder sb, LinkedListNode<Tag> self) {
            sb.Append("</em>");
            return self.Next;
        }

        public override Tag Create() {
            return new ITagClose();
        }
    }
}

