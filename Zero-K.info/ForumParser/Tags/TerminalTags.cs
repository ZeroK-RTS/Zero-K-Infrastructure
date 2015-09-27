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
            // split document to <p> paragraphs using newlines ..
            if (context.ParagraphOpen)
            {
                context.Append("</p>");
                context.ParagraphOpen = false;
            }
            else
            {
                if (self.Next.FirstNode(x => x.Value is NewLineTag) != null)
                {
                    context.ParagraphOpen = true; 
                    context.Append("<p>");
                } else context.Append("<br/>"); // if no matching endline exists, just use br 
            }

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

        public LiteralTag() {}

        public LiteralTag(string str) {
            this.content.Append(str);
        }

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            var csr = content.ToString();

            if (csr.IsValidLink())
            { // implicit linkification and imagifination
                if (csr.EndsWith(".png") || csr.EndsWith(".gif") || csr.EndsWith(".jpg") || csr.EndsWith(".jpeg"))
                {
                    context.AppendFormat("<a href=\"{0}\" target=\"_blank\" ><img src=\"{0}\" max-width=\"100%\" height=\"auto\"/></a>", csr);
                } else context.AppendFormat("<a href=\"{0}\">{0}</a>", csr); 
            }
            else context.Append(HttpUtility.HtmlEncode(content));

            return self.Next;
        }

        public override Tag Create() {
            return new LiteralTag();
        }
    }
}