using System;

namespace ZeroKWeb.ForumParser
{
    public abstract class OpeningArgsTag<T> : ScanningArgsTag, IOpeningTag
    {
        public virtual Type ClosingTagType => typeof(T);
    }
}