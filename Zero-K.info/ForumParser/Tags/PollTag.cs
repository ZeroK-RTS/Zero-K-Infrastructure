using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Linq;
using ZkData;

namespace ZeroKWeb.ForumParser
{
    public class PollOpenTag: OpeningTag<PollCloseTag>
    {
        public override string Match { get; } = "[poll]";

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            var closing = self.NextNodeOfType<PollCloseTag>();
            if (context.Html != null)
            {
                var content = self.Next.GetOriginalContentUntilNode(closing);
                int pollID;
                if (!string.IsNullOrEmpty(content) && int.TryParse(content, out pollID))
                {
                    // Was Html.Action("Index", "Poll", new { pollID }) - a child action, and the
                    // one call site of the seven that is linked C# rather than a view. A BBCode
                    // tag cannot invoke a view component, so this does what PollController.Index
                    // does: load the poll and render its partial.
                    //
                    // PartialString, not Partial: TranslateContext.Append is Append(object) onto
                    // a StringBuilder, so the value is ToString()d. On MVC 5 Html.Partial returns
                    // an MvcHtmlString whose ToString IS the html; on ASP.NET Core it returns an
                    // IHtmlContent whose ToString is the type name. The same trap the grid cells
                    // hit - see AppCode/HtmlCompat.cs.
                    var poll = new ZkDataContext().Polls.FirstOrDefault(x => x.PollID == pollID);
                    if (poll != null) context.Append(context.Html.PartialString("~/Views/Poll/PollView.cshtml", poll));
                }
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