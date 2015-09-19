using System.Collections.Generic;
using System.Text;

namespace PlasmaShared.ForumParser
{
    public class BTagOpen: OpeningTag<BTagClose>
    {
        public override string Match { get; } = "[b]";

        public override void RenderSelf(StringBuilder sb) {
            sb.Append("<b>");
        }
    }


    public class ITagOpen: OpeningTag<ITagClose>
    {
        public override string Match { get; } = "[i]";

        public override void RenderSelf(StringBuilder sb) {
            sb.Append("<i>");
        }
    }

    public class ITagClose: ScanningTag
    {
        public override string Match { get; } = "[/i]";

        public override LinkedListNode<Tag> Translate(StringBuilder sb, LinkedListNode<Tag> self) {
            sb.Append("</i>");
            return self.Next;
        }
    }
}