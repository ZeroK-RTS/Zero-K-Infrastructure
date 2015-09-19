using System;
using System.Collections.Generic;
using System.Text;

namespace PlasmaShared.ForumParser
{
    public abstract class TerminalTag: Tag
    {
        protected StringBuilder Content = new StringBuilder();

        public abstract TerminalTag Create();

        public virtual void Append(char part)
        {
            Content.Append(part);
        }
    }

    public class SpaceTag: TerminalTag
    {
        public override bool? ScanLetter(char letter) {
            return letter == ' ' || letter == '\t';
        }

        public override LinkedListNode<Tag> Translate(StringBuilder sb, LinkedListNode<Tag> self) {
            sb.Append(Content);
            return self.Next;
        }

        public override TerminalTag Create() {
            return new SpaceTag();
        }
    }

    public class NewLineTag: TerminalTag
    {
        public override bool? ScanLetter(char letter) {
            return letter == '\n';
        }

        public override LinkedListNode<Tag> Translate(StringBuilder sb, LinkedListNode<Tag> self) {
            sb.Append("<br/>");
            return self.Next;
        }

        public override TerminalTag Create() {
            return new NewLineTag();
        }
    }



    public class LiteralTag: TerminalTag
    {
        public override bool? ScanLetter(char letter) {
            return true;
        }

        public override LinkedListNode<Tag> Translate(StringBuilder sb, LinkedListNode<Tag> self) {
            sb.Append(Content); // todo sanitize
            return self.Next;
        }

        public override TerminalTag Create() {
            return new LiteralTag();
        }
    }
}