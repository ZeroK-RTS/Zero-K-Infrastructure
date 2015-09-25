using System.Collections.Generic;
using System.Text;
using System.Web.Mvc;

namespace ZeroKWeb.ForumParser
{
    // spoiler tags: [spoiler]spoiler_text[/spoiler]
    // becomes: [Spoiler] and onClick displays the inner Content "spoiler_text" 
    // (taken from jquery.expand.js -> "post score below threshold")
    public class SpoilerOpenTag: OpeningTag<SpoilerCloseTag>
    {
        public override string Match { get; } = "[spoiler]";

        public override LinkedListNode<Tag> Translate(StringBuilder sb, LinkedListNode<Tag> self, HtmlHelper html) {
            sb.AppendFormat(
                "<small class=\"expand\"><a nicetitle-processed=\"Expand/Collapse\" style=\"display:block\" href=\"#\">[Spoiler]</a></small><div style=\"display: none;\" class=\"collapse\">");
            return self.Next;
        }

        public override Tag Create() => new SpoilerOpenTag();
    }

    public class SpoilerCloseTag: ScanningTag
    {
        public override string Match { get; } = "[/spoiler]";

        public override LinkedListNode<Tag> Translate(StringBuilder sb, LinkedListNode<Tag> self, HtmlHelper html) {
            sb.Append("</div>");
            return self.Next;
        }

        public override Tag Create() => new SpoilerCloseTag();
    }
}