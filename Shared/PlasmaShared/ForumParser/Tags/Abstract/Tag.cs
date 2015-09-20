using System.Collections.Generic;
using System.Text;

namespace PlasmaShared.ForumParser
{
    public abstract class Tag
    {
        public abstract bool? ScanLetter(char letter);

        public abstract LinkedListNode<Tag> Translate(StringBuilder sb, LinkedListNode<Tag> self);

        public abstract Tag Create();
    }
}