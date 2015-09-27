using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc.Html;
using Microsoft.Ajax.Utilities;
using ZkData;

namespace ZeroKWeb.ForumParser
{
    public class WikiLink: ScanningArgsTag
    {
        public override string Match { get; } = "[";
        public override char MatchTerminator { get; } = ']';


        public override bool? ScanLetter(ParseContext context, char letter) {
            if (letter == '\r' || letter == '\n') return false;
            return base.ScanLetter(context, letter);
        }

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            var parts = args.ToString().Split(new[] { ' ' }, 2);
            var name = parts.Length > 1 ? parts[1] : parts[0];
            var link = parts[0];
            if (link.IsValidLink()) context.AppendFormat("<a href=\"{0}\">{1}</a>", link, name);
            else context.Append(context.Html?.ActionLink(name, "Index", "Wiki", new { node = link }, null));

            return self.Next;
        }

        public override Tag Create() => new WikiLink();

        protected override bool ValidateArgs() {
            var parts = args.ToString().Split(' ');
            if (string.IsNullOrEmpty(parts[0])) return false;
            using (var db = new ZkDataContext()) return parts[0].IsValidLink() || db.ForumCategories.First(x=>x.IsWiki).ForumThreads.Any(x => x.Title == parts[0]);
        }
    }
}