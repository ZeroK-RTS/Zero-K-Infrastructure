using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using JetBrains.Annotations;
using ZkData;

namespace ZeroKWeb.ForumParser
{
    /// <summary>
    ///     Provides context for translating single page/forum post to html
    /// </summary>
    public class TranslateContext
    {
        readonly StringBuilder sb = new StringBuilder();

        readonly List<TocEntry> tocEntries = new List<TocEntry>();

        int? tocPosition;

        HashSet<string> wikiKeyCache = new HashSet<string>();

        public bool IsWikiKey(string key) {
            if (wikiKeyCache==null) wikiKeyCache = new HashSet<string>(new ZkDataContext().ForumThreads.Where(x=>x.WikiKey!=null).Select(x=>x.WikiKey));
            return wikiKeyCache.Contains(key);
        }

        public TranslateContext(HtmlHelper html) {
            Html = html;
        }

        /// <summary>
        ///     Html helper to be used by translation
        /// </summary>
        public HtmlHelper Html { get; private set; }

        /// <summary>
        ///     Appends raw html
        /// </summary>
        /// <param name="str"></param>
        public void Append(object str) {
            sb.Append(str);
        }

        /// <summary>
        ///     Adds entry for table of content (headers)
        /// </summary>
        /// <param name="entry"></param>
        public void AddTocEntry(TocEntry entry) {
            tocEntries.Add(entry);
        }


        public void AppendToc() {
            tocPosition = sb.Length;
        }

        /// <summary>
        ///     Appends raw html
        /// </summary>
        /// <param name="formatString"></param>
        /// <param name="args"></param>
        [StringFormatMethod("formatString")]
        public void AppendFormat(string formatString, params object[] args) {
            sb.AppendFormat(formatString, args);
        }

        public void InsertAt(int position, string text) {
            sb.Insert(position, text);
        }

        /// <summary>
        ///     do final tweaks - render TOC
        /// </summary>
        public void FinishRendering() {
            if (tocPosition != null) InsertAt(tocPosition.Value, WikiTocTag.RenderToc(tocEntries));
        }

        /// <summary>
        ///     Returns resulting Html
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return sb.ToString();
        }
    }
}