using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc.Html;
using Microsoft.Ajax.Utilities;
using ZkData;

namespace ZeroKWeb.ForumParser
{
    /// <summary>
    /// [WikiKey display name]
    /// [ValidUrl display name]
    /// </summary>
    public class WikiLinkTag: ScanningArgsTag
    {
        public override string Match { get; } = "[";
        public override char MatchTerminator { get; } = ']';


        public override bool? AcceptsLetter(ParseContext context, char letter) {
            if (letter == '\r' || letter == '\n') return false;
            return base.AcceptsLetter(context, letter);
        }

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            var parts = arguments.Split(new[] { ' ' }, 2);
            var name = parts.Length > 1 ? parts[1] : parts[0];
            var link = parts[0];
            if (link.IsValidLinkOrRelativeUrl()) context.AppendFormat("<a href=\"{0}\">{1}</a>", link, name);
            else context.Append(context.Html?.ActionLink(name, "Index", "Wiki", new { node = link }, null));

            return self.Next;
        }

        public override Tag Create() => new WikiLinkTag();

        protected override bool ValidateArgs(ParseContext context, string args) {
            var parts = args.Split(' ');
            if (string.IsNullOrEmpty(parts[0])) return false;
            using (var db = new ZkDataContext()) return parts[0].IsValidLinkOrRelativeUrl() || context.IsWikiKey(parts[0]);
        }
    }
}