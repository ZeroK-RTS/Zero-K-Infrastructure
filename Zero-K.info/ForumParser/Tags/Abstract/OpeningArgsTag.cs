using System;

namespace ZeroKWeb.ForumParser
{
    public abstract class OpeningArgsTag<T> : ScanningArgsTag
    {
        public override OpeningClosingMode Mode { get; } =  OpeningClosingMode.Opening;
        public override bool IsClosedBy(Tag closer) => closer?.GetType() == typeof(T);
    }
}