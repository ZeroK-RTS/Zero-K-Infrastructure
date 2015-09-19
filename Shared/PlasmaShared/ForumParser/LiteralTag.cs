using System.Collections.Generic;
using System.Text;

namespace PlasmaShared.ForumParser
{
    public class LiteralTag: Tag
    {
        StringBuilder content;

        public LiteralTag(string content) {
            this.content = new StringBuilder(content);
        }

        public void Append(string part) {
            content.Append(part);
        }

        public override bool? ScanLetter(char letter) {
            return true;
        }

        public override LinkedListNode<Tag> Translate(StringBuilder sb, LinkedListNode<Tag> self) {
            sb.Append(content);
            return self.Next;
        }
    }
}