using System;
using System.Collections.Generic;
using System.Reflection;
using System.Web.Mvc;

namespace ZeroKWeb.ForumParser
{
    public class ForumWikiParser
    {
        static readonly List<Tag> nonterminalTags = new List<Tag>();
        static readonly List<TerminalTag> terminalTags = new List<TerminalTag> { new NewLineTag(), new SpaceTag(), new LiteralTag() };

        readonly ParseContext context = new ParseContext();

        static ForumWikiParser() {
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


        public string TranslateToHtml(string input, HtmlHelper html) {
            var tags = Parse(input);
            return RenderTags(tags, html);
        }


        /// <summary>
        ///     Parses input string to tag list
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public LinkedList<Tag> Parse(string input) {
            return RemoveNewlinesInHtml(EliminateUnclosedTags(SplitIntoTags(input)));
        }

        LinkedList<Tag> SplitIntoTags(string input) {
            var candidates = new List<Tag>();
            if (string.IsNullOrEmpty(input)) return new LinkedList<Tag>();

            context.Setup(input);

            while (context.Pos < input.Length)
            {
                var letter = context.CurrentChar;

                var mc = 0;

                if (candidates.Count > 0)
                {
                    for (var ci = candidates.Count - 1; ci >= 0; ci--)
                    {
                        var c = candidates[ci];
                        var ret = c.AcceptsLetter(context, letter);
                        if (ret == true)
                        {
                            context.AddTag(c.Create().Init(context.MatchedString));
                            candidates.Clear();
                            context.ResetNonterminalPos();
                            mc = 1;
                            break;
                        }
                        if (ret == null) mc++;
                        else candidates.RemoveAt(ci);
                    }
                } else
                {
                    for (var ci = nonterminalTags.Count - 1; ci >= 0; ci--)
                    {
                        var c = nonterminalTags[ci];
                        var ret = c.AcceptsLetter(context, letter);
                        if (ret == true)
                        {
                            context.AddTag(c.Create().Init(context.MatchedString));
                            context.ResetNonterminalPos();
                            mc = 1;
                            break;
                        }
                        if (ret == null)
                        {
                            mc++;
                            candidates.Add(c);
                        }
                    }
                }

                if (mc == 0 || context.Pos == input.Length - 1) // we are not matching any nonterminal tags
                {
                    ParseTerminals(context);
                    context.ResetNonterminalPos();
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
        static string RenderTags(LinkedList<Tag> tags, HtmlHelper html) {
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
        static LinkedList<Tag> EliminateUnclosedTags(LinkedList<Tag> input) {
            var openedTagsStack = new Stack<Tag>();
            var toDel = new List<Tag>();

            foreach (var tag in input)
            {
                if (tag.Mode == OpeningClosingMode.Opening && (openedTagsStack.Count == 0 || !openedTagsStack.Peek().EscapesContent)) openedTagsStack.Push(tag);
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

            while (openedTagsStack.Count > 0) // replace extra opening tags with literals
            {
                var pop = openedTagsStack.Pop();
                input.Find(pop).Value = new LiteralTag(pop.Text);
            }

            return input;
        }


        /// <summary>
        ///     Removes newlines from within html tags
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        static LinkedList<Tag> RemoveNewlinesInHtml(LinkedList<Tag> input) {
            var htmlLevel = 0;

            foreach (var tag in input)
            {
                if (tag.Mode == OpeningClosingMode.Opening && tag is HtmlTag) htmlLevel++;
                else if (tag.Mode == OpeningClosingMode.Closing && tag is HtmlTag) htmlLevel--;
                else if (tag is NewLineTag) if (htmlLevel > 0) input.Find(tag).Value = new LiteralTag(tag.Text);
            }
            return input;
        }

        /// <summary>
        ///     Parses terminal symbols - like string constants
        /// </summary>
        static void ParseTerminals(ParseContext context) {
            for (var i = context.NonterminalStartPos; i <= context.Pos; i++)
            {
                var scanChar = context.Input[i];

                var lastTerm = context.PreviousTag?.Value as TerminalTag;
                if (lastTerm == null || lastTerm.AcceptsLetter(context, scanChar) == false)
                {
                    foreach (var tt in terminalTags)
                    {
                        if (tt.AcceptsLetter(context, scanChar) != false)
                        {
                            context.AddTag(((TerminalTag)tt.Create()).AppendChar(scanChar));
                            break;
                        }
                    }
                } else lastTerm.AppendChar(scanChar);
            }
        }
    }
}