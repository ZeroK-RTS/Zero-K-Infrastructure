using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ZeroKWeb.ForumParser
{
    /// <summary>
    ///     allows safe/secured html tags to go into code
    /// </summary>
    public class HtmlTag: Tag
    {
        static readonly Dictionary<string, List<string>> validTags = new Dictionary<string, List<string>>
        {
            ["a"] = new List<string> { "href", "title", "nicetitle" },
            ["b"] = new List<string>(),
            ["br"] = new List<string>(),
            ["blockquote"] = new List<string>(),
            ["code"] = new List<string>(),
            ["dd"] = new List<string>(),
            ["div"] = new List<string> { "title", "nicetitle" },
            ["dl"] = new List<string>(),
            ["dt"] = new List<string>(),
            ["em"] = new List<string>(),
            ["font"] = new List<string> { "face", "size", "color" },
            ["h1"] = new List<string>(),
            ["h2"] = new List<string>(),
            ["h3"] = new List<string>(),
            ["h4"] = new List<string>(),
            ["h5"] = new List<string>(),
            ["h6"] = new List<string>(),
            ["hr"] = new List<string>(),
            ["i"] = new List<string>(),
            ["img"] = new List<string> { "src", "alt", "border", "height", "width", "align", "title", "nicetitle" },
            ["li"] = new List<string>(),
            ["ol"] = new List<string> { "type", "start" },
            ["p"] = new List<string> { "align" },
            ["pre"] = new List<string>(),
            ["q"] = new List<string>(),
            ["s"] = new List<string>(),
            ["span"] = new List<string> { "nicetitle", "title" },
            ["strike"] = new List<string>(),
            ["strong"] = new List<string>(),
            ["sub"] = new List<string>(),
            ["sup"] = new List<string>(),
            ["table"] = new List<string> { "align", "valign", "cellspacing", "cellpadding", "border", "width", "height" },
            ["tbody"] = new List<string> { "align", "valign", "cellspacing", "cellpadding", "border", "width", "height" },
            ["td"] = new List<string> { "align", "valign", "cellspacing", "cellpadding", "border", "width", "height" },
            ["tfoot"] = new List<string> { "align", "valign", "cellspacing", "cellpadding", "border", "width", "height" },
            ["th"] = new List<string> { "align", "valign", "cellspacing", "cellpadding", "border", "width", "height" },
            ["thead"] = new List<string> { "align", "valign", "cellspacing", "cellpadding", "border", "width", "height", "colspan", "rowspan" },
            ["tr"] = new List<string> { "align", "valign", "cellspacing", "cellpadding", "border", "width", "height" },
            ["tt"] = new List<string>(),
            ["u"] = new List<string>(),
            ["ul"] = new List<string> { "type" },
            ["var"] = new List<string>()
        };

        string htmlTag;

        OpeningClosingMode mode;
        public override OpeningClosingMode Mode => mode;


        public override bool? AcceptsLetter(ParseContext context, char letter) {
            if (letter == '\r' || letter == '\n') return false; // don't allow multiline html
            if (context.MatchedString.Length == 1 && context.MatchedString[0] != '<') return false;
            if (context.MatchedString.Length == 2 && context.MatchedString[1] == ' ') return false;

            if (context.MatchedString.Length > 2 && letter == '>')
            {
                var tag = Regex.Match(context.MatchedString, "</?([a-z0-9]+)[ />]",RegexOptions.IgnoreCase).Groups[1].Value;

                // invalid opening tag
                if (!validTags.ContainsKey(tag)) return false;

                if (context.MatchedString.StartsWith("</")) return true; // for closing elements, end here

                var csr = context.MatchedString;
                if (!csr.EndsWith("/>")) csr = $"{csr}</{tag}>"; // close tag for xelement parsing
                try
                {
                    XElement.Parse(csr);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return null;
        }

        public override bool IsClosedBy(Tag closer) {
            var c = (closer as HtmlTag);
            return c != null && c.htmlTag == htmlTag && mode == OpeningClosingMode.Opening && c.mode == OpeningClosingMode.Closing;
        }

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            context.Append(Text);
            return self.Next;
        }

        public override Tag Init(string text) {
            if (text.EndsWith("/>")) mode = OpeningClosingMode.SelfClosed;
            else if (text.StartsWith("</")) mode = OpeningClosingMode.Closing;
            else mode = OpeningClosingMode.Opening;

            htmlTag = Regex.Match(text, "</?([a-z]+)[ />]").Groups[1].Value;

            if (mode == OpeningClosingMode.Closing)
            {
                Text = $"</{htmlTag}>";
                return this;
            }

            if (mode == OpeningClosingMode.Opening) text = $"{text}</{htmlTag}>"; // close tag for xelement parsing

            var parsed = XElement.Parse(text);
            var validAttrs = validTags[htmlTag];

            // remove invalid attributes
            foreach (var attr in parsed.Attributes().ToList())
            {
                var name = attr.Name.ToString();

                if (!validAttrs.Contains(name)) parsed.SetAttributeValue(attr.Name, null);
                if ((name == "src" || name == "href") && !attr.Value.IsValidLinkOrRelativeUrl()) parsed.SetAttributeValue(attr.Name, null);
            }

            text = parsed.ToString(); // turn back to string
            if (mode == OpeningClosingMode.Opening) text = text.Substring(0, text.Length - 3 - htmlTag.Length);
            Text = text;
            return this;
        }

        public override Tag Create() => new HtmlTag();
    }
}