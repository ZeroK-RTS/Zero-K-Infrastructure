using System.Text;

namespace ZeroKWeb.ForumParser
{
    public abstract class ScanningTag: Tag
    {
        public abstract string Match { get; }

        public override bool? AcceptsLetter(ParseContext context, char letter) {
            if (Match[context.MatchedString.Length-1] != context.CurrentCharLowerCase) return false;
            if (context.MatchedString.Length == Match.Length) return true;
            return null;
        }
    }
}