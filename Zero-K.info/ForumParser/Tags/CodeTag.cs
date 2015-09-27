
using System;
using System.Collections.Generic;
using System.Web;

namespace ZeroKWeb.ForumParser
{
    public class CodeOpenTag:OpeningTag<CodeCloseTag>
    {
        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            var closing = self.FirstNode(x => x.Value is CodeCloseTag);
            var str = self.Next.GetOriginalContentUntilNode(closing);
            context.AppendFormat("<pre>{0}</pre>", HttpUtility.HtmlEncode(str));
            return closing.Next;
        }

        public override Tag Create() => new CodeOpenTag();

        public override string Match { get; } = "{{{";
    }

    public class CodeCloseTag:ClosingTag
    {
        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            throw new ApplicationException("This should never be executed");
        }

        public override Tag Create() => new CodeCloseTag();

        public override string Match { get; } = "}}}";
    }
}