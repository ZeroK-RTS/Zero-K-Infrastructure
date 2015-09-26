using System.Collections.Generic;
using System.Text;
using System.Web.Mvc;

namespace ZeroKWeb.ForumParser
{
    public abstract class Tag {
        public virtual OpeningClosingMode Mode { get; } = OpeningClosingMode.SelfClosed;
        public virtual bool IsClosedBy(Tag closer) => false;
        public abstract bool? ScanLetter(ParseContext context, char letter);
        public abstract LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self);
        public abstract Tag Create();
        public abstract string GetOriginalContent();
    }
}