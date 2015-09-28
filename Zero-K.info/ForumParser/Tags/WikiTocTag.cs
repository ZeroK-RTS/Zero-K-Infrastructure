using System.Collections.Generic;
using System.Text;

namespace ZeroKWeb.ForumParser
{
    /// <summary>
    ///     Shows some list of headers within the document
    /// </summary>
    public class WikiTocTag: ScanningArgsTag
    {
        public override string Match { get; } = "<wiki:toc";
        public override char MatchTerminator { get; } = '>';

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            context.AppendToc();
            return self.Next;
        }

        public override Tag Create() => new WikiTocTag();

        public override bool? AcceptsLetter(ParseContext context, char letter) {
            if (letter == '\r' || letter == '\n') return false;
            return base.AcceptsLetter(context, letter);
        }

        public static string RenderToc(IEnumerable<TocEntry> tocEntries) {
            var sb = new StringBuilder();

            var level = 0;

            foreach (var t in tocEntries)
            {
                if (t.Level > level) for (var i = 0; i < t.Level - level; i++) sb.Append("<ul>");
                if (t.Level < level) for (var i = 0; i < level - t.Level; i++) sb.Append("</ul>");
                sb.AppendFormat("<li><a href='#{0}'/>{1}</a></li>", t.LinkAnchor, t.Name);
                level = t.Level;
            }
            for (var i = 0; i < level; i++) sb.Append("</ul>");

            return sb.ToString();
        }

        protected override bool ValidateArgs(ParseContext context, string args) => true;
    }
}