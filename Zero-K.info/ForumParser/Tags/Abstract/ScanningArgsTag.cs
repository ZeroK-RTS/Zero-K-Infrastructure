using System.Text;

namespace ZeroKWeb.ForumParser
{
    public abstract class ScanningArgsTag: Tag
    {
        public abstract string Match { get; }
        public abstract char MatchTerminator { get; }
        protected string arguments;
        
        public override bool? AcceptsLetter(ParseContext context, char letter) {
            if (context.MatchedString.Length > Match.Length)
            {
                if (letter == MatchTerminator)
                {
                    if (ValidateArgs(context, ExtractArgs(context.MatchedString))) return true;
                    return false;
                }
                if (letter == '\n') return false;
            } else if (Match[context.MatchedString.Length-1] != context.CurrentCharLowerCase) return false;

            return null;
        }

        public virtual string ExtractArgs(string fullMatch) {
            return fullMatch.Substring(Match.Length, fullMatch.Length - Match.Length - 1);
        }

        public override Tag Init(string text) {
            base.Init(text);
            arguments = ExtractArgs(text);
            return this;
        }

        protected abstract bool ValidateArgs(ParseContext context, string args);

        
    }
}