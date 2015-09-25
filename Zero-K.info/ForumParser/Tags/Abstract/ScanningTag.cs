using System.Text;

namespace ZeroKWeb.ForumParser
{
    public abstract class ScanningTag: Tag
    {
        protected int pos;
        public abstract string Match { get; }
        protected StringBuilder matchedString = new StringBuilder();

        public override bool? ScanLetter(char letter) {
            matchedString.Append(letter);
            if (char.ToLower(Match[pos++]) != char.ToLower(letter)) return false;
            if (pos == Match.Length) return true;
            return null;
        }

        public override string GetOriginalContent() => matchedString.ToString();
    }
}