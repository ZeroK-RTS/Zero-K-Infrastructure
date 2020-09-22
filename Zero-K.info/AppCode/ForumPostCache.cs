using System.Collections.Concurrent;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Runtime.Remoting.Messaging;
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

        ConcurrentDictionary<string,MvcHtmlString>  cache = new ConcurrentDictionary<string, MvcHtmlString>();

        void ZkDataContextOnAfterEntityChange(object sender, ZkDataContext.EntityEntry dbEntityEntry) {
            // Invalidates cache on entity changes
            var post = dbEntityEntry.Entity as ForumPost;
            if (dbEntityEntry.State != EntityState.Added && dbEntityEntry.State != EntityState.Modified) return;
            if (post != null)
            {
                MvcHtmlString dummy;
                cache.TryRemove(GetKey(post, true), out dummy);
                cache.TryRemove(GetKey(post, false), out dummy);
            } else
            {
                var news = dbEntityEntry.Entity as News;
                if (news != null)
                {
                    MvcHtmlString dummy;
                    cache.TryRemove(GetKey(news, true), out dummy);
                    cache.TryRemove(GetKey(news, false), out dummy);
                }
            }
        }

        private static string GetKey(ForumPost p, bool? overrideModerator = null) {
            return "p" + p.ForumPostID + "#" + (overrideModerator ?? Global.IsModerator);
        }

        private static string GetKey(News n, bool? overrideModerator = null) {
            return "n" + n.NewsID + "#" + (overrideModerator ?? Global.IsModerator);
        }

        public MvcHtmlString GetCachedHtml(ForumPost post, HtmlHelper html) {
            MvcHtmlString parsed;
            if (cache.TryGetValue(GetKey(post), out parsed)) return parsed;
            else
            {
                var parser =new ForumWikiParser();
                parsed = new MvcHtmlString(parser.TranslateToHtml(post.Text, html));
                cache[GetKey(post)] = parsed;
            }
            return parsed;
        }

        public MvcHtmlString GetCachedHtml(News post, HtmlHelper html)
        {
            MvcHtmlString parsed;
            if (cache.TryGetValue(GetKey(post), out parsed)) return parsed;
            else
            {
                var parser = new ForumWikiParser();
                parsed = new MvcHtmlString(parser.TranslateToHtml(post.Text, html));
                cache[GetKey(post)] = parsed;
            }
            return parsed;
        }

    }
}