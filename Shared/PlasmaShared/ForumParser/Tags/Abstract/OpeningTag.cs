using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace PlasmaShared.ForumParser
{
    public abstract class OpeningTag<T>: ScanningTag where T:Tag
    {
        public abstract void RenderSelf(StringBuilder sb);

        public override LinkedListNode<Tag> Translate(StringBuilder sb, LinkedListNode<Tag> self) {

            // check if there is corresponding closing tag
            /*var node = self.Next;
            int openingTags = 0;
            int closingTags = 0;
            while (node != null)
            {
                if (node.Value.GetType() == GetType()) openingTags++;
                if (node.Value is T) closingTags++;
                node = node.Next;
            }
            
            if (closingTags-openingTags>0) RenderSelf(sb);*/

            RenderSelf(sb);
            
            return self.Next;
        }
    }
}