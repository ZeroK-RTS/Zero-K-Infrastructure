using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace ZeroKWeb.ForumParser
{
    public class PollOpenTag: OpeningTag<PollCloseTag>
    {
        public override string Match { get; } = "[poll]";

        public override LinkedListNode<Tag> Translate(StringBuilder sb, LinkedListNode<Tag> self, HtmlHelper html) {
            var closing = self.NextNodeOfType<PollOpenTag>();
            if (html != null)
            {
                var content = self.Next.GetOriginalContentUntilNode(closing);
                int pollID;
                if (!string.IsNullOrEmpty(content) && int.TryParse(content, out pollID)) sb.Append(html.Action("Index", "Poll", new { pollID }));
            }

            return closing.Next;
        }

        public override Tag Create() => new PollOpenTag();
    }

    public class PollCloseTag: ScanningTag
    {
        public override string Match { get; } = "[/poll]";

        public override LinkedListNode<Tag> Translate(StringBuilder sb, LinkedListNode<Tag> self, HtmlHelper html) {
            throw new NotImplementedException(); // should not be executed
        }

        public override Tag Create() => new PollCloseTag();
    }
}