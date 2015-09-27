using System.Collections.Generic;
using System.Text;
using System.Web;

namespace ZeroKWeb.ForumParser
{
    public abstract class TerminalTag: Tag
    {
        protected StringBuilder content = new StringBuilder();

        public override string GetOriginalContent() => content.ToString();
    }

    public class SpaceTag: TerminalTag
    {
        public override bool? ScanLetter(ParseContext context, char letter) {
            if (letter == ' ' || letter == '\t')
            {
                content.Append(letter);
                return true;
            }
            return false;
        }

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            context.Append(content);
            return self.Next;
        }

        public override Tag Create() => new SpaceTag();
    }

    public class NewLineTag: TerminalTag
    {
        public override bool? ScanLetter(ParseContext context, char letter) {
            if (letter == '\n' || letter == '\r')
            {
                if (content.ToString().Contains("\n")) return false; // allow only one \n in the same tag
                content.Append(letter);
                return true;
            }
            return false;
        }

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            // split document to <p> paragraphs using newlines ..
            if (!context.ParagraphOpen) context.Append("<p>");
            else context.Append("</p><p>");
            return self.Next;
        }

        public override Tag Create() => new NewLineTag();
    }


    public class LiteralTag: TerminalTag
    {
        public LiteralTag() {}

        public LiteralTag(string str) {
            content.Append(str);
        }

        public override bool? ScanLetter(ParseContext context, char letter) {
            if (letter == ' ' || letter == '\t' || letter == '\r' || letter == '\n') return false;
            content.Append(letter);
            return true;
        }

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            var csr = content.ToString();

            if (csr.IsValidLink())
            {
                // implicit linkification and imagifination
                if (csr.EndsWith(".png") || csr.EndsWith(".gif") || csr.EndsWith(".jpg") || csr.EndsWith(".jpeg")) context.AppendFormat("<a href=\"{0}\" target=\"_blank\" ><img src=\"{0}\" max-width=\"100%\" height=\"auto\"/></a>", csr);
                else context.AppendFormat("<a href=\"{0}\">{0}</a>", csr);
            } else context.Append(HttpUtility.HtmlEncode(content));

            return self.Next;
        }

        public override Tag Create() => new LiteralTag();
    }
}