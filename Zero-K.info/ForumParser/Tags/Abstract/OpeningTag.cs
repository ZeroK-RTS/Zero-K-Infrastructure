using System;

namespace ZeroKWeb.ForumParser
{
    public abstract class OpeningTag<T>: ScanningTag, IOpeningTag
    {
        public virtual Type ClosingTagType => typeof(T);
    }
}