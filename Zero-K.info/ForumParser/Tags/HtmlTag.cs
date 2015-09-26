using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        StringBuilder content = new StringBuilder();

        string htmlTag;

        OpeningClosingMode mode;
        public override OpeningClosingMode Mode => mode;


        public override bool? ScanLetter(ParseContext context, char letter) {
            content.Append(letter);

            if (letter == '\r' || letter == '\n') return false; // don't allow multiline html
            if (content[0] != '<') return false;

            if (htmlTag == null && content.Length > 2)
            {
                if (letter == ' ' || letter == '/' || letter == '>')
                {
                    if (content.ToString().StartsWith("</"))
                    {
                        mode = OpeningClosingMode.Closing;
                        htmlTag = content.ToString(2, content.Length - 3);
                        return letter == '>' && validTags.ContainsKey(htmlTag); // for closing elements, end here
                    }
                    mode = OpeningClosingMode.Opening;
                    htmlTag = content.ToString(1, content.Length - 2);
                    if (!validTags.ContainsKey(htmlTag)) return false;
                }
            }

            if (htmlTag != null)
            {
                var csr = content.ToString();
                if (csr.EndsWith(">"))
                {
                    if (csr.EndsWith("/>")) mode = OpeningClosingMode.SelfClosed;
                    else csr = $"{csr}</{htmlTag}>"; // close tag for xelement parsing
                    try
                    {
                        var parsed = XElement.Parse(csr);
                        var validAttrs = validTags[htmlTag];

                        // remove invalid attributes
                        foreach (var attr in parsed.Attributes().ToList())
                        {
                            var name = attr.Name.ToString();

                            if (!validAttrs.Contains(name)) parsed.SetAttributeValue(attr.Name, null);
                            if ((name == "src" || name == "href") && !attr.Value.IsValidLink()) parsed.SetAttributeValue(attr.Name, null);
                        }

                        csr = parsed.ToString(); // turn back to string
                        if (mode == OpeningClosingMode.Opening) csr = csr.Substring(0, csr.Length - 3 - htmlTag.Length);
                        content = new StringBuilder(csr);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }

            return null;
        }

        public override bool IsClosedBy(Tag closer) {
            var c = (closer as HtmlTag);
            return c != null && c.htmlTag == htmlTag && mode == OpeningClosingMode.Opening && c.mode == OpeningClosingMode.Closing;
        }

        public override LinkedListNode<Tag> Translate(TranslateContext context, LinkedListNode<Tag> self) {
            context.Append(content);
            return self.Next;
        }

        public override Tag Create() => new HtmlTag();


        public override string GetOriginalContent() => content.ToString();
    }
}