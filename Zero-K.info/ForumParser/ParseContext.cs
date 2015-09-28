using System.Collections.Generic;
using System.Linq;
using ZkData;

namespace ZeroKWeb.ForumParser
{
    public class ParseContext
    {
        HashSet<string> wikiKeyCache = new HashSet<string>();
        public LinkedList<Tag> Tags { get; private set; } = new LinkedList<Tag>();


        public char CurrentLetter { get; private set; }
        public int Pos { get; private set; }
        public string Input { get; private set; }
        public LinkedListNode<Tag> PreviousTag => Tags.Last;
        public char? NextChar { get; private set; }
        public string MatchedString { get; private set; }
        public int ScanStartPos { get; private set; }

        public void Setup(string input) {
            Tags = new LinkedList<Tag>();
            Pos = 0;
            Input = input;
            CurrentLetter = Input[Pos];
            NextChar = Input.Length > Pos + 1 ? Input[Pos + 1] : (char?)null;
            MatchedString = input.Substring(0, 1);
            ScanStartPos = 0;
        }

        public bool IsWikiKey(string key) {
            if (wikiKeyCache == null) wikiKeyCache = new HashSet<string>(new ZkDataContext().ForumThreads.Where(x => x.WikiKey != null).Select(x => x.WikiKey));
            return wikiKeyCache.Contains(key);
        }

        public void ResetScanPos() {
            ScanStartPos = Pos + 1;
            MatchedString = Input.Substring(ScanStartPos, Pos - ScanStartPos + 1);
        }

        public void AdvancePos() {
            Pos++;
            if (Pos < Input.Length)
            {
                NextChar = Input.Length > Pos + 1 ? Input[Pos + 1] : (char?)null;
                CurrentLetter = Input[Pos];
                MatchedString = Input.Substring(ScanStartPos, Pos - ScanStartPos + 1);
            }
        }

        public void AddTag(Tag tag) {
            Tags.AddLast(tag);
        }
    }
}