using System;

namespace ZeroKWeb.ForumParser
{
    public abstract class OpeningTag: ScanningTag
    {
        public abstract Type ClosingTagType { get; }
    }

    public abstract class OpeningTag<T>: OpeningTag
    {
        public override Type ClosingTagType => typeof(T);
    }
}