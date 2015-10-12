namespace ZeroKWeb
{
    public interface IUniGridCol
    {
        string Description { get; }
        string ID { get; }
        bool AllowsSort { get; }
        bool AllowWeb { get; }
        bool AllowCsv { get; }
        bool IsSelector { get; }
        string Width { get; }
    }
}