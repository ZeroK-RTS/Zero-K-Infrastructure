using System.Collections.Generic;
using System.Text;
using System.Web;
using Microsoft.Ajax.Utilities;

namespace ZeroKWeb.ForumParser
{
    public abstract class TerminalTag: Tag
    {
        protected StringBuilder sb = new StringBuilder();

        public override string Text => sb.ToString();

        public TerminalTag AppendChar(char c) 
        {
            sb.Append(c);
            return this;
        }

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self)
        {
            context.Append(Text);
            return self.Next;
        }
    }

    public class SpaceTag: TerminalTag
    {
        public override bool? AcceptsLetter(ParseContext context, char letter) {
            if (letter == ' ' || letter == '\t') { return true;}
            return false;
        }

        public override Tag Create() => new SpaceTag();
    }

    public class NewLineTag: TerminalTag
    {
        public override bool? AcceptsLetter(ParseContext context, char letter) {
            if (letter == '\n' || letter == '\r')
            {
                if (sb.ToString().Contains("\n")) return false; // allow only one \n in the same tag
                return true;
            }
            return false;
        }

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            context.Append("<br/>");
            return self.Next;
        }

        public override Tag Create() => new NewLineTag();
    }


    public class LiteralTag: TerminalTag
    {
        public LiteralTag() {}

        public LiteralTag(string str) {
            sb.Append(str);
        }

        public override bool? AcceptsLetter(ParseContext context, char letter) {
            if (letter == ' ' || letter == '\t' || letter == '\r' || letter == '\n') return false;
            return true;
        }

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            if (Text.IsValidLink())
            {
                // implicit linkification and imagifination
                if (Text.EndsWith(".png") || Text.EndsWith(".gif") || Text.EndsWith(".jpg") || Text.EndsWith(".jpeg")) context.AppendFormat("<a href=\"{0}\" target=\"_blank\" ><img src=\"{0}\" max-width=\"100%\" height=\"auto\"/></a>", Text);
                else context.AppendFormat("<a href=\"{0}\">{0}</a>", Text);
            } else context.Append(HttpUtility.HtmlEncode(Text));

            return self.Next;
        }

        public override Tag Create() => new LiteralTag();
    }
}