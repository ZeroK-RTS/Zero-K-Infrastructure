namespace ZeroKWeb.ForumParser
{
    public abstract class ClosingTag: ScanningTag
    {
        public override OpeningClosingMode Mode { get; } = OpeningClosingMode.Closing;
    }
}