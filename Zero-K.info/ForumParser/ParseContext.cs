using System.Collections.Generic;

namespace ZeroKWeb.ForumParser
{
    public class ParseContext
    {
        public ParseContext(int pos, string input, LinkedListNode<Tag> previousTag) {
            Pos = pos;
            Input = input;
            PreviousTag = previousTag;
            NextChar = Input.Length > Pos + 1 ? Input[Pos + 1] : (char?)null;
        }

        public int Pos { get; }
        public string Input { get; }
        public LinkedListNode<Tag> PreviousTag { get; }
        public char? NextChar { get; private set; }

    }
}