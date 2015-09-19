using System.Collections.Generic;
using System.Text;

namespace PlasmaShared.ForumParser
{
    public class BTagClose: ScanningTag
    {
        public override string Match { get; } = "[/b]";

        public override LinkedListNode<Tag> Translate(StringBuilder sb, LinkedListNode<Tag> self) {
            sb.Append("</b>");
            return self.Next;
        }
    }
}