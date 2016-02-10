using System.Collections.Generic;
using System.Text;
using System.Web;
using ZkData;

namespace ZeroKWeb.ForumParser
{
    /// <summary>
    ///     = header =  or == header ==  generates h1 to h6 headers along with anchor for table of content
    /// </summary>
    public class HeaderTag: Tag
    {
        public override bool? AcceptsLetter(ParseContext context, char letter) {
            if (letter == '=')
            {
                if (context.MatchedString.Length > 6) return false; // max is h6

                if (context.NextChar != '=')
                {
                    if (context.PreviousTag == null || context.PreviousTag.Value is SpaceTag || context.PreviousTag.Value is NewLineTag ||
                        context.NextChar == null || context.NextChar == ' ' || context.NextChar == '\r' || context.NextChar == '\n') return true;
                    return false;
                }

                return null;
            } else return false;
        }

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            var level = Text.Length;

            var ender = self.Next.FirstNode(x => x.Value is HeaderTag || x.Value is NewLineTag);
            if (ender?.Value is HeaderTag && ender.Value.Text.Length == level)
            {
                var name = HttpUtility.HtmlEncode(self.Next.GetOriginalContentUntilNode(ender).Trim());
                var link =  Account.StripInvalidLobbyNameChars(name.Replace(" ", "_").Replace("\"", "_").Replace("'", "_"));
                context.AppendFormat("<h{0}><a name=\"{1}\"></a>{2}</h{0}>", level, link, name);
                context.AddTocEntry(new TocEntry(name, link, level));

                return ender.Next;
            }

            context.Append(Text);
            return self.Next;
        }

        public override Tag Create() => new HeaderTag();
    }
}