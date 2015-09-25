using System.Text;
using System.Web.Mvc;
using JetBrains.Annotations;

namespace ZeroKWeb.ForumParser
{
    /// <summary>
    ///     Provides context for translating single page/forum post to html
    /// </summary>
    public class TranslateContext
    {
        readonly StringBuilder sb = new StringBuilder();

        public TranslateContext(HtmlHelper html)
        {
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
        public void Append(object str)
        {
            sb.Append(str);
        }


        /// <summary>
        ///     Appends raw html
        /// </summary>
        /// <param name="formatString"></param>
        /// <param name="args"></param>
        [StringFormatMethod("formatString")]
        public void AppendFormat(string formatString, params object[] args)
        {
            sb.AppendFormat(formatString, args);
        }

        /// <summary>
        ///     Returns resulting Html
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return sb.ToString();
        }
    }
}