using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Mvc;

namespace ZeroKWeb.ForumParser
{
    // embed player to display videos (supported: mp4, 3gp, webm, ogg, ogv, gifv)
    public class VidOpenTag: OpeningTag<VidCloseTag>
    {
        public override string Match { get; } = "[vid]";

        public override LinkedListNode<Tag> Translate(StringBuilder sb, LinkedListNode<Tag> self, HtmlHelper html) {
            var closing = self.NextNodeOfType<VidCloseTag>();

            var content = self.Next.GetOriginalContentUntilNode(closing);
            if (!string.IsNullOrEmpty(content))
            {
                var match = Regex.Match(content, @"(https?\:\/\/)((\w|-|_|\.|\/)+\.)(mp4|webm|ogg|ogv|3gp|gifv)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var urlPart = match.Groups[1].Value + match.Groups[2].Value;
                    sb.AppendFormat(
                        "<div class=\"video-container\"><video preload=\"auto\" height=\"auto\" width=\"auto\" controls=\"controls\"><source type=\"video/webm\" src=\"{0}webm\"><source type=\"video/mp4\" src=\"{0}mp4\"><source type=\"video/ogg\" src=\"{0}ogg\"><source type=\"video/ogv\" src=\"{0}ogv\"><source type=\"video/3gp\" src=\"{0}3gp\">Your browser does not support the video tag. Find out if your Browser is supported at www.w3schools.com/tags/tag_video.asp</video></div>",
                        urlPart);
                }
            }

            return closing.Next;
        }

        public override Tag Create() => new VidOpenTag();
    }

    public class VidCloseTag: ScanningTag
    {
        public override string Match { get; } = "[/vid]";

        public override LinkedListNode<Tag> Translate(StringBuilder sb, LinkedListNode<Tag> self, HtmlHelper html) {
            throw new ApplicationException("This isn't happening"); // should not be called
        }

        public override Tag Create() => new VidCloseTag();
    }
}