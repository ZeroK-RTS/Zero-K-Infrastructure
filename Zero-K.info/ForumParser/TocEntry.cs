namespace ZeroKWeb.ForumParser
{
    public class TocEntry
    {
        public string Name { get; }
        public string LinkAnchor { get; }
        public int Level { get; }
        public TocEntry(string name, string linkAnchor, int level) {
            Name = name;
            LinkAnchor = linkAnchor;
            Level = level;
        }
    }
}