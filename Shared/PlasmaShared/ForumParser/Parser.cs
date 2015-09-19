using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlasmaShared.ForumParser
{
    public class Parser
    {
        public string Parse(string input) {
            var candidates = new List<Tag> { new BTagClose(), new BTagOpen() };

            var tags = new LinkedList<Tag>();

            var pos = 0;
            var scanStart = 0;

            while (pos < input.Length)
            {
                var letter = input[pos];

                foreach (var c in candidates.ToList())
                {
                    var ret = c.ScanLetter(letter);
                    if (ret == true)
                    {
                        tags.AddLast(c);
                        candidates.Clear();
                        scanStart = pos + 1;
                    } else if (ret == false) candidates.Remove(c);
                }

                if (candidates.Count == 0)
                {
                    if (pos - scanStart >= 0)
                    {
                        var rootLit = tags.Last?.Value as LiteralTag;
                        if (rootLit != null) rootLit.Append(input.Substring(scanStart, pos - scanStart + 1));
                        else
                        {
                            var lit = new LiteralTag(input.Substring(scanStart, pos - scanStart + 1));
                            tags.AddLast(lit);
                        }
                    }
                    scanStart = pos + 1;
                    candidates.Add(new BTagOpen());
                    candidates.Add(new BTagClose());
                }
                pos++;
            }

            var sb = new StringBuilder();
            var node = tags.First;
            while (node != null) node = node.Value.Translate(sb, node);

            return sb.ToString();
        }
    }
}