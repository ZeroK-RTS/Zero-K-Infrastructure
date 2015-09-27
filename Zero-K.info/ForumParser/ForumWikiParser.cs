using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web.Mvc;

namespace ZeroKWeb.ForumParser
{
    public class ForumWikiParser
    {
        static readonly List<Tag> nonterminalTags = new List<Tag>();
        static readonly List<TerminalTag> terminalTags = new List<TerminalTag> { new NewLineTag(), new SpaceTag(), new LiteralTag() };

        static ForumWikiParser()
        {
            // load all classes using reflection
            foreach (var t in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (typeof(Tag).IsAssignableFrom(t) && !t.IsAbstract && !typeof(TerminalTag).IsAssignableFrom(t))
                {
                    var tag = (Tag)t.GetConstructor(new Type[] { }).Invoke(null);
                    nonterminalTags.Add(tag);
                }
            }
        }

        List<Tag> InitNonTerminals()
        {
#if DEBUG
            var ret = new List<Tag>(nonterminalTags.Count);
            foreach (var nt in nonterminalTags)
            {
                var created = nt.Create();
                if (created.GetType() != nt.GetType()) throw new ApplicationException("Each parser tag must create its own clone");
                ret.Add(created);
            }
            return ret;
#else
            return nonterminalTags.Select(x => x.Create()).ToList();
#endif
        }

        public string ProcessToHtml(string input, HtmlHelper html)
        {
            var tags = ParseToTags(input);
            return RenderTags(tags, html);
        }

        /// <summary>
        ///     Parses input string to tag list
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        LinkedList<Tag> ParseToTags(string input)
        {
            var candidates = InitNonTerminals();

            var tags = new LinkedList<Tag>();

            var pos = 0;
            var scanStart = 0;

            while (pos < input.Length)
            {
                var letter = input[pos];
                var context = new ParseContext(pos, input, tags.Last);

                foreach (var c in candidates.ToList())
                {
                    var ret = c.ScanLetter(context, letter);
                    if (ret == true)
                    {
                        tags.AddLast(c);
                        candidates.Clear();
                        scanStart = pos + 1;
                    }
                    else if (ret == false) candidates.Remove(c);
                }

                if (candidates.Count == 0) // we are not matching any nonterminal tags
                {
                    if (pos - scanStart >= 0) ParseTerminals(input, scanStart, pos, tags);
                    scanStart = pos + 1;
                    candidates = InitNonTerminals();
                }
                pos++;
            }

            return tags;
        }

        /// <summary>
        ///     Renders final tags to html string builder
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        static string RenderTags(LinkedList<Tag> tags, HtmlHelper html)
        {
            var context = new TranslateContext(html);

            tags = EliminateUnclosedTags(tags);

            var node = tags.First;
            while (node != null) node = node.Value.Translate(context, node);
            context.FinishRendering();

            return context.ToString();
        }


        /// <summary>
        ///     Elimintes unclosed tags or unopened tags like [b] without closing [/b]
        /// </summary>
        /// <param name="input">parsed tags</param>
        /// <returns></returns>
        static LinkedList<Tag> EliminateUnclosedTags(LinkedList<Tag> input)
        {
            var openedTagsStack = new Stack<Tag>();
            var toDel = new List<Tag>();

            foreach (var tag in input)
            {
                if (tag.Mode == OpeningClosingMode.Opening) openedTagsStack.Push(tag);
                else if (tag.Mode == OpeningClosingMode.Closing)
                {
                    if (openedTagsStack.Count == 0) toDel.Add(tag);
                    else
                    {
                        var peek = openedTagsStack.Peek();
                        if (peek.IsClosedBy(tag)) openedTagsStack.Pop();
                        else toDel.Add(tag);
                    }
                }
            }

            foreach (var td in toDel) input.Find(td).Value = new LiteralTag(td.GetOriginalContent()); // replace extra closing tags with literals

            while (openedTagsStack.Count > 0)  // replace extra opening tags with literals
            {
                var pop = openedTagsStack.Pop();
                input.Find(pop).Value = new LiteralTag(pop.GetOriginalContent());
            }

            return input;
        }

        /// <summary>
        ///     Parses terminal symbols - like string constants
        /// </summary>
        /// <param name="input">string to be prcessed</param>
        /// <param name="scanStart">start position</param>
        /// <param name="pos">end position (included)</param>
        /// <param name="tags">current tags linked list to be added to</param>
        static void ParseTerminals(string input, int scanStart, int pos, LinkedList<Tag> tags)
        {
            for (var i = scanStart; i <= pos; i++)
            {
                var scanChar = input[i];

                var context = new ParseContext(pos, input, tags.Last); // match so far not set correctly here

                var lastTerm = tags.Last?.Value as TerminalTag;
                if (lastTerm == null || lastTerm.ScanLetter(context, scanChar) == false)
                {
                    tags.AddLast(terminalTags.Select(x => x.Create()).First(x => x.ScanLetter(context, scanChar) != false));
                }
            }
        }
    }
}