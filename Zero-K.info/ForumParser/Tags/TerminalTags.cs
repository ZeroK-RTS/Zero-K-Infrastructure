using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace ZeroKWeb.ForumParser
{
    public abstract class TerminalTag: Tag
    {
        protected StringBuilder content = new StringBuilder();

        public virtual void Append(char part) {
            content.Append(part);
        }

        public override string GetOriginalContent() => content.ToString();
    }

    public class SpaceTag: TerminalTag
    {
        public override bool? ScanLetter(ParseContext context, char letter) {
            return letter == ' ' || letter == '\t';
        }

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            context.Append(content);
            return self.Next;
        }

        public override Tag Create() {
            return new SpaceTag();
        }
    }

    public class NewLineTag: TerminalTag
    {
        public override bool? ScanLetter(ParseContext context, char letter) {
            return letter == '\n' || letter=='\r';
        }

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            context.Append("<br/>");
            return self.Next;
        }

        public override Tag Create() {
            return new NewLineTag();
        }
    }


    public class LiteralTag: TerminalTag
    {
        public override bool? ScanLetter(ParseContext context, char letter) {
            return true;
        }

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            if (ForumWikiParser.IsValidLink(content.ToString())) context.AppendFormat("<a href=\"{0}\">{0}</a>", content); // implicit linkification
            else context.Append(HttpUtility.HtmlEncode(content));

            return self.Next;
        }

        public override Tag Create() {
            return new LiteralTag();
        }
    }
}