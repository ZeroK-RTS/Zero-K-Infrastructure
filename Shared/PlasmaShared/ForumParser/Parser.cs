using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Services.Description;

namespace PlasmaShared.ForumParser
{
    public class Parser
    {
        public List<Tag> InitCandidates() {
            var ret = new List<Tag>();
            foreach (var t in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (typeof(Tag).IsAssignableFrom(t) && t != typeof(LiteralTag) && !t.IsAbstract && !typeof(TerminalTag).IsAssignableFrom(t))
                {
                    var tag = (Tag)t.GetConstructor(new Type[] { }).Invoke(null);
                    ret.Add(tag);
                }
            }
            return ret;
        }

        public string Parse(string input) {
            var candidates = InitCandidates();
            
            var tags = new LinkedList<Tag>();
            var terminals = new List<TerminalTag>() { new NewLineTag(), new SpaceTag(), new LiteralTag() }; // order matters

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

                if (candidates.Count == 0) // we are not matching any tags
                {
                    if (pos - scanStart >= 0)
                    {
                        for (int i = scanStart; i <= pos; i++)
                        {
                            var scanChar = input[i];
                            var term = terminals.First(x => x.ScanLetter(scanChar) == true);
                            var lastTerm = tags.Last?.Value as TerminalTag;

                            if (lastTerm?.GetType() == term.GetType())
                            {
                                lastTerm.Append(scanChar);
                            } else
                            {
                                term = (TerminalTag)term.Create(); // create fresh instance
                                term.Append(scanChar);
                                tags.AddLast(term);
                            }

                        }
                    }
                    scanStart = pos + 1;
                    candidates = InitCandidates();
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