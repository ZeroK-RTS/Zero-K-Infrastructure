using System.Collections.Generic;
using System.Text;
using System.Web.Mvc;

namespace ZeroKWeb.ForumParser
{
    public abstract class Tag
    {
        public abstract bool? ScanLetter(char letter);
        public abstract LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self);
        public abstract Tag Create();
        public abstract string GetOriginalContent();
    }
}