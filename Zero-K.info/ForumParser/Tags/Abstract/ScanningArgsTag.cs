using System.Text;

namespace ZeroKWeb.ForumParser
{
    public abstract class ScanningArgsTag: Tag
    {
        protected StringBuilder args = new StringBuilder();
        protected int pos;
        public abstract string Match { get; }
        public abstract char MatchTerminator { get; }

        public override bool? ScanLetter(char letter) {
            if (pos >= Match.Length)
            {
                if (letter == MatchTerminator)
                {
                    if (ValidateArgs()) return true;
                    return false;
                }
                if (letter == '\n') return false;
                args.Append(letter);
            } else if (char.ToLower(Match[pos++]) != char.ToLower(letter)) return false;

            return null;
        }

        protected abstract bool ValidateArgs();
    }
}