using System.Collections.Generic;
using System.Text;
using System.Web.Mvc;
using ZkData.Migrations;

namespace ZeroKWeb.ForumParser
{
    public abstract class Tag {
        public virtual OpeningClosingMode Mode { get; } = OpeningClosingMode.SelfClosed;

        /// <summary>
        /// for tags like {{{ code }}}
        /// </summary>
        public virtual bool EscapesContent { get; } = false;


        public virtual bool IsClosedBy(Tag closer) => false;
        public abstract bool? AcceptsLetter(ParseContext context, char letter);
        public abstract LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self);

        public abstract Tag Create();

        public virtual string Text { get; protected set; }

        public virtual Tag Init(string text) 
        {
            Text = text;
            return this;
        }
    }
}