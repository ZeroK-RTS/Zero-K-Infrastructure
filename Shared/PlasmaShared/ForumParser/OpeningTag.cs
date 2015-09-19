using System.Collections.Generic;
using System.Text;

namespace PlasmaShared.ForumParser
{
    public abstract class OpeningTag<T>: ScanningTag where T:Tag
    {
        public abstract void RenderSelf(StringBuilder sb);

        public override LinkedListNode<Tag> Translate(StringBuilder sb, LinkedListNode<Tag> self) {
            RenderSelf(sb);

            var nextNode = self.Next;
            while (nextNode != null)
            {
                nextNode.Value.Translate(sb, nextNode);
                if (nextNode.Value is T) return nextNode.Next;
                nextNode = nextNode.Next;
            }
            return nextNode;
        }
    }
}