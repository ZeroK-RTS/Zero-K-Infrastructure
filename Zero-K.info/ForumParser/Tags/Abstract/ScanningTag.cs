namespace ZeroKWeb.ForumParser
{
    public abstract class ScanningTag: Tag
    {
        protected int pos;
        public abstract string Match { get; }

        public override bool? ScanLetter(char letter) {
            if (char.ToLower(Match[pos++]) != char.ToLower(letter)) return false;
            if (pos == Match.Length) return true;
            return null;
        }
    }
}