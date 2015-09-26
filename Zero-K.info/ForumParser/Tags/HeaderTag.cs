using System.Collections.Generic;
using System.Text;

namespace ZeroKWeb.ForumParser
{
    public class HeaderTag: Tag
    {
        readonly StringBuilder content = new StringBuilder();

        public override bool? ScanLetter(ParseContext context, char letter) {
            if (letter == '=')
            {
                if (context.PreviousTag == null || context.PreviousTag.Value is SpaceTag || context.PreviousTag.Value is NewLineTag)
                {
                    content.Append(letter);
                    if (context.NextChar == null || context.NextChar == ' ' || context.NextChar == '\n' || context.NextChar=='\r') return true;
                    return null;
                }
            }
            return false;
        }

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            var prev = self.Previous;
            var level = GetOriginalContent().Length;

            if ((prev == null || prev.Value is SpaceTag) && self.Next?.Value is SpaceTag)
            {
                var ender = self.Next.FirstNode(x => x.Next == null || x.Next.Value is NewLineTag);
                if (ender.Value is SpaceTag) ender = ender.Previous;
                var hend = ender?.Value as HeaderTag;

                if (hend != null && hend.GetOriginalContent().Length == level && ender.Previous?.Value is SpaceTag)
                {
                    var name = self.Next.Next.GetOriginalContentUntilNode(ender.Previous);
                    var link = name.Replace(" ", "_").Replace("\"", "_").Replace("'", "_");

                    context.AppendFormat("<a name=\"{1}\"><h{0}>", level, link);
                    self.Next.Next.TranslateUntilNode(context, ender.Previous);
                    context.AppendFormat("</h{0}></a>", level);

                    context.AddTocEntry(new TocEntry(name, link, level));

                    return ender.Next;
                }
            }

            context.Append(content);
            return self.Next;
        }

        public override Tag Create() => new HeaderTag();

        public override string GetOriginalContent() => content.ToString();
    }
}