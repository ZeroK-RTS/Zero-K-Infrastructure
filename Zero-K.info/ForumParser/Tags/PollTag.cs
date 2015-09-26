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

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            var closing = self.NextNodeOfType<PollOpenTag>();
            if (context.Html != null)
            {
                var content = self.Next.GetOriginalContentUntilNode(closing);
                int pollID;
                if (!string.IsNullOrEmpty(content) && int.TryParse(content, out pollID)) context.Append(context.Html.Action("Index", "Poll", new { pollID }));
            }

            return closing.Next;
        }

        public override Tag Create() => new PollOpenTag();
    }

    public class PollCloseTag: ClosingTag
    {
        public override string Match { get; } = "[/poll]";

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            throw new NotImplementedException(); // should not be executed
        }

        public override Tag Create() => new PollCloseTag();
    }
}