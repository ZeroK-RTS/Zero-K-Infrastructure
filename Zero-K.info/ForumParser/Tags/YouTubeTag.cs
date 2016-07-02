using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Mvc;

namespace ZeroKWeb.ForumParser
{
    // embed YouTube
    public class YouTubeOpenTag: OpeningTag<YouTubeCloseTag>
    {
        public override string Match { get; } = "[youtube]";

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            var closing = self.NextNodeOfType<YouTubeCloseTag>();

            var content = self.Next.GetOriginalContentUntilNode(closing);
            if (!string.IsNullOrEmpty(content))
            {
                var match = Regex.Match(content, "v=([^&]+)");
                if (match.Success)
                {
                    context.AppendFormat("<iframe width=\"420\" height=\"315\" src=\"http://www.youtube.com/embed/{0}\" frameborder=\"0\" hd=\"1\" allowfullscreen=\"1\"></iframe>", match.Groups[1].Value);
                    return closing.Next;
                }
                match = Regex.Match(content, @"\.be/([^&]+)");
                if (match.Success)
                {
                    context.AppendFormat("<iframe width=\"420\" height=\"315\" src=\"http://www.youtube.com/embed/{0}\" frameborder=\"0\" hd=\"1\" allowfullscreen=\"1\"></iframe>", match.Groups[1].Value);
                    return closing.Next;
                }
            }

            return closing.Next;
        }

        public override Tag Create() => new YouTubeOpenTag();
    }

    public class YouTubeCloseTag: ClosingTag
    {
        public override string Match { get; } = "[/youtube]";

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            throw new ApplicationException("This isn't happening"); // should not be called
        }

        public override Tag Create() => new YouTubeCloseTag();
    }
}