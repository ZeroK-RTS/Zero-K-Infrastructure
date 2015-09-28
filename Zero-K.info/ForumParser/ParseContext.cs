using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZkData;

namespace ZeroKWeb.ForumParser
{
    public class ParseContext
    {
        readonly StringBuilder sb = new StringBuilder();
        HashSet<string> wikiKeyCache;
        public LinkedList<Tag> Tags { get; private set; } = new LinkedList<Tag>();

        public char CurrentChar { get; private set; }
        public int Pos { get; private set; }
        public string Input { get; private set; }
        public LinkedListNode<Tag> PreviousTag => Tags.Last;
        public char? NextChar { get; private set; }
        public char CurrentCharLowerCase { get; private set; }

        public string MatchedString { get; private set; }
        public int NonterminalStartPos { get; private set; }

        public void Setup(string input) {
            Tags = new LinkedList<Tag>();
            Pos = 0;
            Input = input;
            CurrentChar = Input[Pos];
            NextChar = Input.Length > Pos + 1 ? Input[Pos + 1] : (char?)null;
            CurrentCharLowerCase = CurrentChar.ToLower();
            sb.Clear();
            sb.Append(CurrentChar);
            MatchedString = sb.ToString();
            NonterminalStartPos = 0;
        }

        public bool IsWikiKey(string key) {
            if (wikiKeyCache == null) wikiKeyCache = new HashSet<string>(new ZkDataContext().ForumThreads.Where(x => x.WikiKey != null).Select(x => x.WikiKey));
            return wikiKeyCache.Contains(key);
        }

        public void ResetNonterminalPos() {
            NonterminalStartPos = Pos + 1;
            sb.Clear();
            MatchedString = string.Empty;
        }

        public void AdvancePos() {
            Pos++;
            if (Pos < Input.Length)
            {
                NextChar = Input.Length > Pos + 1 ? Input[Pos + 1] : (char?)null;
                CurrentChar = Input[Pos];
                CurrentCharLowerCase = CurrentChar.ToLower();
                sb.Append(CurrentChar);
                MatchedString = sb.ToString();
            }
        }

        public void AddTag(Tag tag) {
            Tags.AddLast(tag);
        }
    }
}