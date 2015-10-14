using System.Collections.Concurrent;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using ZeroKWeb.ForumParser;
using ZkData;

namespace ZeroKWeb
{
    public class ForumPostIndexer
    {
        readonly ForumWikiParser parser = new ForumWikiParser();

        readonly ConcurrentDictionary<string, int> wordIDs = new ConcurrentDictionary<string, int>();

        public ForumPostIndexer() {
            using (var db = new ZkDataContext())
            {
                if (!db.IndexWords.Any())
                {
                    foreach (var post in db.ForumPosts)
                    {
                        IndexPost(post);
                    }
                }
            }
            ZkDataContext.AfterEntityChange += ZkDataContextOnAfterEntityChange;
        }

        int GetWordID(string word) {
            int id;
            if (wordIDs.TryGetValue(word, out id)) return id;
            using (var db = new ZkDataContext())
            {
                var entry = db.IndexWords.FirstOrDefault(x => x.Text == word);
                if (entry == null)
                {
                    entry = new Word { Text = word };
                    db.IndexWords.Add(entry);
                    db.SaveChanges();
                }
                wordIDs[entry.Text] = entry.WordID;
                return entry.WordID;
            }
        }

        void ZkDataContextOnAfterEntityChange(object sender, DbEntityEntry dbEntityEntry) {
            var post = dbEntityEntry.Entity as ForumPost;
            if (post != null && dbEntityEntry.State != EntityState.Deleted) IndexPost(post);
        }

        public static string SanitizeWord(string word) {
            return Account.StripInvalidLobbyNameChars(word).ToLower();
        }

        public void IndexPost(ForumPost post) {
            var words =
                ForumWikiParser.EliminateUnclosedTags(parser.ParseToTags(post.Text)).Where(x => x is LiteralTag && x.Text?.Length < 100).Select(x=>SanitizeWord(x.Text)).Where(x=>!string.IsNullOrEmpty(x)).ToList();

            using (var db = new ZkDataContext())
            {
                var dbPost = db.ForumPosts.Find(post.ForumPostID);
                dbPost.ForumPostWords.Clear();

                foreach (var grp in words.GroupBy(x => x)) dbPost.ForumPostWords.Add(new ForumPostWord { Count = grp.Count(), WordID = GetWordID(grp.Key) });
                db.SaveChanges();
            }
        }
    }
}