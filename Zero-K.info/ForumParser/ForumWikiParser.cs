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


        public string ProcessToHtml(string input, HtmlHelper html)
        {
            var tags = ParseToTags(input);
            return RenderTags(tags, html);
        }

        ParseContext context = new ParseContext();
        
        /// <summary>
        ///     Parses input string to tag list
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        LinkedList<Tag> ParseToTags(string input) {

            var candidates = new List<Tag>(nonterminalTags);

            context.Setup(input);

            while (context.Pos < input.Length)
            {
                var letter = context.CurrentLetter;
                
                for (int ci = candidates.Count-1; ci>=0 ; ci--) {
                    var c = candidates[ci];
                    var ret = c.ScanLetter(context, letter);
                    if (ret == true)
                    {
                        context.AddTag(c.Create().Init(context.MatchedString));
                        candidates = new List<Tag>(nonterminalTags);
                        context.ResetScanPos();
                        break;
                    }
                    else if (ret == false) candidates.RemoveAt(ci);
                }

                if (candidates.Count == 0) // we are not matching any nonterminal tags
                {
                    ParseTerminals(context);
                    context.ResetScanPos();
                    candidates = new List<Tag>(nonterminalTags);
                }

                context.AdvancePos();
            }

            return context.Tags;
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

            foreach (var td in toDel) input.Find(td).Value = new LiteralTag(td.Text); // replace extra closing tags with literals

            while (openedTagsStack.Count > 0)  // replace extra opening tags with literals
            {
                var pop = openedTagsStack.Pop();
                input.Find(pop).Value = new LiteralTag(pop.Text);
            }

            return input;
        }

        /// <summary>
        ///     Parses terminal symbols - like string constants
        /// </summary>
        static void ParseTerminals(ParseContext context) {
            for (var i = context.ScanStartPos; i <= context.Pos; i++)
            {
                var scanChar = context.Input[i];

                var lastTerm = context.PreviousTag?.Value as TerminalTag;
                if (lastTerm == null || lastTerm.ScanLetter(context, scanChar) == false)
                {
                    context.AddTag(terminalTags.Select(x => (TerminalTag)x.Create()).First(x => x.ScanLetter(context, scanChar) != false).AppendChar(scanChar));

                } else
                {
                    lastTerm.AppendChar(scanChar);
                }
            }
        }
    }
}