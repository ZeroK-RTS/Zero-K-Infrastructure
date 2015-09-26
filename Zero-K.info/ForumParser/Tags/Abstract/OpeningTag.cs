namespace ZeroKWeb.ForumParser
{
    public abstract class OpeningTag<T>: ScanningTag
    {
        public override OpeningClosingMode Mode { get; } = OpeningClosingMode.Opening;
        public override bool IsClosedBy(Tag closer) => closer?.GetType() == typeof(T);
    }
}