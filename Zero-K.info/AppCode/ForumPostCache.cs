using System.Collections.Concurrent;
using System.Data.Entity.Infrastructure;
using System.Web.Mvc;
using ZeroKWeb.ForumParser;
using ZkData;

namespace ZeroKWeb
{
    /// <summary>
    /// Caches html of forum posts for greater performance
    /// </summary>
    public class ForumPostCache
    {
        public ForumPostCache() {
            ZkDataContext.AfterEntityChange += ZkDataContextOnAfterEntityChange;
        }

        ConcurrentDictionary<int,MvcHtmlString>  cache = new ConcurrentDictionary<int, MvcHtmlString>();

        void ZkDataContextOnAfterEntityChange(object sender, DbEntityEntry dbEntityEntry) {
            var post = dbEntityEntry.Entity as ForumPost;
            if (post != null) cache[post.ForumPostID] = null;
        }

        public MvcHtmlString GetCachedHtml(ForumPost post, HtmlHelper html) {
            MvcHtmlString parsed;
            if (cache.TryGetValue(post.ForumPostID, out parsed)) return parsed;
            else
            {
                var parser =new ForumWikiParser();
                parsed = new MvcHtmlString(parser.ProcessToHtml(post.Text, html));
                cache[post.ForumPostID] = parsed;
            }
            return parsed;
        }
    }
}