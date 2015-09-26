using System.Collections.Generic;
using System.Text;
using System.Web;

namespace ZeroKWeb.ForumParser
{
    /// <summary>
    ///     = header =  or == header ==  generates h1 to h6 headers along with anchor for table of content
    /// </summary>
    public class HeaderTag: Tag
    {
        readonly StringBuilder content = new StringBuilder();

        public override bool? ScanLetter(ParseContext context, char letter) {
            if (letter == '=')
            {
                if (context.PreviousTag == null || context.PreviousTag.Value is SpaceTag || context.PreviousTag.Value is NewLineTag)
                {
                    content.Append(letter);
                    if (content.Length > 6) return false; // max is h6
                    if (context.NextChar == null || context.NextChar == ' ' || context.NextChar == '\n' || context.NextChar == '\r') return true;
                    return null;
                }
            }
            return false;
        }

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            var level = GetOriginalContent().Length;

            var ender = self.Next.FirstNode(x => x.Value is HeaderTag || x.Value is NewLineTag);
            if (ender?.Value is HeaderTag)
            {
                var name = HttpUtility.HtmlEncode(self.Next.GetOriginalContentUntilNode(ender).Trim());
                var link = name.Replace(" ", "_").Replace("\"", "_").Replace("'", "_");

                context.AppendFormat("<h{0}>{2}<a name=\"{1}\"></a></h{0}>", level, link, name);
                context.AddTocEntry(new TocEntry(name, link, level));

                return ender.Next;
            }

            context.Append(content);
            return self.Next;
        }

        public override Tag Create() => new HeaderTag();

        public override string GetOriginalContent() => content.ToString();
    }
}