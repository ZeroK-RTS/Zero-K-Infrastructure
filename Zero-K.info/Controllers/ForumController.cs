using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class ForumController: Controller
    {
        public const int PageSize = GlobalConst.ForumPostsPerPage;

        /// <summary>
        ///     Returns false for <see cref="News" /> posts and comment threads on <see cref="Clan" />s, <see cref="Mission" />s,
        ///     PlanetWars <see cref="Planet" />s and <see cref="SpringBattle" />s; true otherwise
        /// </summary>
        /// <param name="thread"></param>
        /// <returns></returns>
        bool IsNormalThread(ForumThread thread) {
            if (thread.Clans != null && thread.Clans.Count > 0) return false;
            if (thread.Missions != null && thread.Missions.Count > 0) return false;
            if (thread.Planets != null && thread.Planets.Count > 0) return false;
            if (thread.SpringBattles != null && thread.SpringBattles.Count > 0) return false;
            if (thread.News != null && thread.News.Count > 0) return false;
            return true;
        }

        /// <summary>
        ///     Set the last post time of the thread to current time (includes edits)
        /// </summary>
        void ResetThreadLastPostTime(int threadID) {
            var db = new ZkDataContext();
            var thread = db.ForumThreads.FirstOrDefault(x => x.ForumThreadID == threadID);
            var lastPost = thread.Created;
            foreach (var p in thread.ForumPosts.Reverse())
            {
                if (p.ForumPostEdits.Count > 0)
                {
                    var lastEdit = p.ForumPostEdits.Last().EditTime;
                    if (lastEdit > lastPost) lastPost = lastEdit;
                } else if (p.Created > lastPost) lastPost = p.Created;
            }
            thread.LastPost = lastPost;
            db.SubmitChanges();
        }

        [Auth(Role = AuthRole.ZkAdmin)]
        public ActionResult DeletePost(int? postID) {
            var db = new ZkDataContext();
            var post = db.ForumPosts.Single(x => x.ForumPostID == postID);
            var thread = post.ForumThread;
            var threadID = thread.ForumThreadID;
            //int index = post.ForumThread.ForumPosts.IndexOf(post);
            var page = GetPostPage(post);

            db.ForumPosts.DeleteOnSubmit(post);
            if (thread.ForumPosts.Count() <= 1 && IsNormalThread(thread))
            {
                db.ForumThreadLastReads.DeleteAllOnSubmit(db.ForumThreadLastReads.Where(x => x.ForumThreadID == thread.ForumThreadID).ToList());
                db.ForumThreads.DeleteOnSubmit(thread);
                db.SubmitChanges();
                return RedirectToAction("Index");
            }
            db.SubmitChanges();
            ResetThreadLastPostTime(threadID);
            return RedirectToAction("Thread", new { id = threadID, page });
        }

        [Auth(Role = AuthRole.ZkAdmin)]
        public ActionResult DeleteAllPostsByUser(int accountID, string accountName) {
            var db = new ZkDataContext();
            var acc = db.Accounts.FirstOrDefault(x => x.AccountID == accountID);
            if (acc.Name != accountName) return Content("Invalid safety code");
            foreach (var p in acc.ForumPosts) DeletePost(p.ForumPostID);
            return RedirectToAction("Index");
        }

        /// <summary>
        ///     Go to forum index, or a subforum
        /// </summary>
        public ActionResult Index(IndexResult model) {
            var db = new ZkDataContext();
            model = model ?? new IndexResult();

            model.Categories = db.ForumCategories.Where(x => x.ParentForumCategoryID == model.CategoryID).OrderBy(x => x.SortOrder);

            model.CurrentCategory = db.ForumCategories.FirstOrDefault(x => x.ForumCategoryID == model.CategoryID);
            model.Path = model.CurrentCategory?.GetPath() ?? new List<ForumCategory>();

            var threads = db.ForumThreads.AsQueryable();

            if (model.CategoryID != null) threads = threads.Where(x => x.ForumCategoryID == model.CategoryID);
            else threads = threads.Where(x => x.ForumCategory.ForumMode != ForumMode.Archive);

            threads = threads.Where(x => x.RestrictedClanID == null || x.RestrictedClanID == Global.ClanID);

            int? filterAccountID = null;
            if (!string.IsNullOrEmpty(model.User))
            {
                filterAccountID =
                    (db.Accounts.FirstOrDefault(x => x.Name == model.User) ?? db.Accounts.FirstOrDefault(x => x.Name.ToLower().Contains(model.User)))?.AccountID;
            }
            if (filterAccountID.HasValue) threads = threads.Where(x => x.CreatedAccountID == filterAccountID || x.ForumPosts.Any(y => y.AuthorAccountID == filterAccountID));

            
            if (model.OnlyUnread && Global.IsAccountAuthorized)
            {
                threads = from t in threads
                    let read = t.ForumThreadLastReads.FirstOrDefault(x => x.AccountID == Global.AccountID)
                    let readForum = t.ForumCategory.ForumLastReads.FirstOrDefault(x => x.AccountID == Global.AccountID)
                    where (read == null || t.LastPost > read.LastRead) && (readForum == null || t.LastPost > readForum.LastRead)
                    select t;
            }

            if (!string.IsNullOrEmpty(model.Search))
            {
                var threadList =
                    Global.ForumPostIndexer.FilterPosts(db.ForumPosts, model.Search).Select(x => x.ForumThreadID).Distinct().Take(1000).ToList();
                threads = threads.Where(x => x.Title.Contains(model.Search) || x.WikiKey.Contains(model.Search) || threadList.Contains(x.ForumThreadID));
            }

            model.Threads = threads.OrderByDescending(x => x.ForumCategoryID == model.CategoryID && x.IsPinned).ThenByDescending(x => x.LastPost);

            return View("ForumIndex", model);
        }


        public ActionResult GetPostList(PostListModel model) {
            var db = new ZkDataContext();
            model = model ?? new PostListModel();

            var thread = db.ForumThreads.First(x => x.ForumThreadID == model.ThreadID && (x.RestrictedClanID == null || x.RestrictedClanID == Global.ClanID));

            var posts = thread.ForumPosts.AsQueryable();

            if (!string.IsNullOrEmpty(model.Search))
            {
                posts = Global.ForumPostIndexer.FilterPosts(posts, model.Search);
            }
            if (!string.IsNullOrEmpty(model.User))
            {
                var filterAccountID = (db.Accounts.FirstOrDefault(x => x.Name == model.User) ?? db.Accounts.FirstOrDefault(x => x.Name.Contains(model.User)))?.AccountID;
                if (filterAccountID.HasValue) posts = posts.Where(x => x.AuthorAccountID == filterAccountID);
            }

            model.Data = posts.OrderBy(x=>x.ForumPostID);
            model.Thread = thread;

            return View("PostList", model);
        }

        public class PostListModel
        {
            public int ThreadID { get; set; }
            public string Search { get; set; }
            public int GoToPost { get; set; }
            public string User { get; set; }
            public ForumThread Thread;
            public IQueryable<ForumPost> Data;
        }


        /// <summary>
        ///     Make a new post or edit an existing one
        /// </summary>
        /// <param name="categoryID">The ID of the subforum the <see cref="ForumPost" /> is/will be in</param>
        /// <param name="threadID">The <see cref="ForumThread" /> ID, if not a new thread</param>
        /// <param name="forumPostID">The <see cref="ForumPost" /> ID, if editing an existing post</param>
        /// <returns></returns>
        [Auth]
        public ActionResult NewPost(int? categoryID, int? threadID, int? forumPostID, string wikiKey) {
            var res = new NewPostResult();
            var db = new ZkDataContext();

            var penalty = Punishment.GetActivePunishment(Global.AccountID, "", 0, x => x.BanForum);
            if (penalty != null)
            {
                return
                    Content(
                        string.Format("You cannot post while banned from forum!\nExpires: {0} UTC\nReason: {1}", penalty.BanExpires, penalty.Reason));
            }

            if (threadID.HasValue && threadID > 0)
            {
                var clan = db.Clans.FirstOrDefault(x => x.ForumThreadID == threadID);
                if (clan != null && Global.ClanID != clan.ClanID) return Content(string.Format("You are not a member of {0}, you cannot post in their clan thread", clan.ClanName));

                var t = db.ForumThreads.Single(x => x.ForumThreadID == threadID.Value);
                res.CurrentThread = t;
                res.LastPosts = res.CurrentThread.ForumPosts.OrderByDescending(x => x.ForumPostID).Take(20);
                if (!categoryID.HasValue) categoryID = t.ForumCategoryID;
            }
            if (!categoryID.HasValue)
            {
                categoryID =
                    db.ForumCategories.Where(x => !x.IsLocked && x.ForumMode == ForumMode.General).OrderBy(x => x.SortOrder).First().ForumCategoryID;
                    // post in general by default
            }

            var category = db.ForumCategories.FirstOrDefault(x => x.ForumCategoryID == categoryID);
            res.Path = category?.GetPath() ?? new List<ForumCategory>();
            
            res.CurrentCategory = category;
            if (forumPostID != null)
            {
                var post = db.ForumPosts.Single(x => x.ForumPostID == forumPostID);
                if (!post.CanEdit(Global.Account)) return Content("You cannot edit this post");
                res.EditedPost = post;
            }
            if (threadID != null)
            {
                var thread = res.CurrentThread;
                res.CanSetTopic = (thread.ForumPosts.Count > 0 && thread.ForumPosts.First().ForumPostID == forumPostID &&
                                   (category.ForumMode == ForumMode.General || category.ForumMode == ForumMode.Wiki || category.ForumMode == ForumMode.Archive));
            } else res.CanSetTopic = true;

            res.WikiKey = wikiKey;

            return View(res);
        }

        /// <summary>
        ///     Try to make a new post or edit an existing one; make a new thread if approriate
        /// </summary>
        /// <param name="threadID">The <see cref="ForumThread" /> ID, if not a new thread</param>
        /// <param name="categoryID">The ID of the subforum the <see cref="ForumPost" /> is/will be in</param>
        /// <param name="forumPostID">The <see cref="ForumPost" /> ID, if editing an existing post</param>
        [Auth]
        [ValidateInput(false)]
        public ActionResult SubmitPost(
            int? threadID,
            int? categoryID,
            int? resourceID,
            int? missionID,
            int? springBattleID,
            int? clanID,
            int? planetID,
            string text,
            string title,
            string wikiKey,
            int? forumPostID) {
            if (threadID == null && missionID == null && resourceID == null && springBattleID == null && clanID == null && planetID == null &&
                forumPostID == null && string.IsNullOrWhiteSpace(title)) return Content("Cannot post new thread with blank title");
            if (string.IsNullOrWhiteSpace(text)) return Content("Please type some text :)");

            var penalty = Punishment.GetActivePunishment(Global.AccountID, "", 0, x => x.BanForum);
            if (penalty != null)
            {
                return
                    Content(
                        string.Format("You cannot post while banned from forum!\nExpires: {0} UTC\nReason: {1}", penalty.BanExpires, penalty.Reason));
            }

            var db = new ZkDataContext();
            using (var scope = new TransactionScope())
            {
                var thread = db.ForumThreads.SingleOrDefault(x => x.ForumThreadID == threadID);
                var category = thread?.ForumCategory;
                if (category == null && categoryID != null) category = db.ForumCategories.FirstOrDefault(x => x.ForumCategoryID == categoryID);
                string currentTitle = null;

                // update title
                if (thread != null && !string.IsNullOrEmpty(title))
                {
                    currentTitle = thread.Title;
                    thread.Title = title;
                    thread.WikiKey = wikiKey;
                }
                if (thread != null && planetID != null)
                {
                    var planet = db.Planets.Single(x => x.PlanetID == planetID);
                    thread.Title = "Planet " + planet.Name;
                }
                if (thread != null && clanID != null)
                {
                    var clan = db.Clans.Single(x => x.ClanID == clanID);
                    thread.Title = "Clan " + clan.ClanName;
                }
                if (thread != null && missionID != null)
                {
                    var mission = db.Missions.Single(x => x.MissionID == missionID);
                    thread.Title = "Mission " + mission.Name;
                }
                if (thread != null && resourceID != null)
                {
                    var map = db.Resources.Single(x => x.ResourceID == resourceID);
                    thread.Title = "Map " + map.InternalName;
                }

                if (threadID == null && category != null) // new thread
                {
                    if (category.IsLocked) return Content("Thread is locked");

                    if (category.ForumMode == ForumMode.Wiki)
                    {
                        if (string.IsNullOrEmpty(wikiKey) || !Account.IsValidLobbyName(wikiKey))
                        {
                            return Content("You need to set a valid wiki key");
                        }
                        if (db.ForumThreads.Any(y => y.WikiKey == wikiKey))
                        {
                            return Content("This wiki key already exists");
                        }
                    }

                    if (string.IsNullOrEmpty(title)) return Content("Title cannot be empty");
                    thread = new ForumThread();
                    thread.CreatedAccountID = Global.AccountID;
                    thread.Title = title;
                    thread.WikiKey = wikiKey;
                    thread.ForumCategoryID = category.ForumCategoryID;
                    db.ForumThreads.InsertOnSubmit(thread);
                }

                if (thread == null && resourceID != null) // non existing thread, we posted new post on map
                {
                    var res = db.Resources.Single(x => x.ResourceID == resourceID);
                    if (res.ForumThread != null) return Content("Double post");
                    thread = new ForumThread
                    {
                        Title = "Map " + res.InternalName,
                        CreatedAccountID = Global.AccountID,
                        LastPostAccountID = Global.AccountID
                    };
                    thread.ForumCategory = db.ForumCategories.FirstOrDefault(x => x.ForumMode == ForumMode.Maps);
                    res.ForumThread = thread;
                    db.ForumThreads.InsertOnSubmit(thread);
                }

                if (thread == null && springBattleID != null) // non existing thread, we posted new post on battle
                {
                    var bat = db.SpringBattles.Single(x => x.SpringBattleID == springBattleID);
                    if (bat.ForumThread != null) return Content("Double post");
                    thread = new ForumThread { Title = bat.FullTitle, CreatedAccountID = Global.AccountID, LastPostAccountID = Global.AccountID };
                    thread.ForumCategory = db.ForumCategories.FirstOrDefault(x => x.ForumMode == ForumMode.SpringBattles);
                    bat.ForumThread = thread;
                    db.ForumThreads.InsertOnSubmit(thread);
                }

                if (thread == null && clanID != null)
                {
                    var clan = db.Clans.Single(x => x.ClanID == clanID);
                    if (clan.ForumThread != null) return Content("Double post");
                    thread = new ForumThread
                    {
                        Title = "Clan " + clan.ClanName,
                        CreatedAccountID = Global.AccountID,
                        LastPostAccountID = Global.AccountID
                    };
                    thread.ForumCategory = db.ForumCategories.FirstOrDefault(x => x.ForumMode == ForumMode.Clans);
                    clan.ForumThread = thread;
                    thread.Clan = clan;
                    db.ForumThreads.InsertOnSubmit(thread);
                }

                if (thread == null && planetID != null)
                {
                    var planet = db.Planets.Single(x => x.PlanetID == planetID);
                    if (planet.ForumThread != null) return Content("Double post");
                    thread = new ForumThread
                    {
                        Title = "Planet " + planet.Name,
                        CreatedAccountID = Global.AccountID,
                        LastPostAccountID = Global.AccountID
                    };
                    thread.ForumCategory = db.ForumCategories.FirstOrDefault(x => x.ForumMode == ForumMode.Planets);
                    planet.ForumThread = thread;
                    db.ForumThreads.InsertOnSubmit(thread);
                }


                if (thread == null) return Content("Thread not found");
                if (thread.IsLocked) return Content("Thread is locked");

                var lastPost = thread.ForumPosts.OrderByDescending(x => x.ForumPostID).FirstOrDefault();

                int? gotoPostId = null;
                //double post preventer
                if (lastPost == null || lastPost.AuthorAccountID != Global.AccountID || lastPost.Text != text ||
                    (!string.IsNullOrEmpty(title) && title != currentTitle))
                {
                    if (forumPostID != null)
                    {
                        var post = thread.ForumPosts.Single(x => x.ForumPostID == forumPostID);
                        if (!post.CanEdit(Global.Account)) throw new ApplicationException("Not authorized to edit the post");
                        post.ForumPostEdits.Add(
                            new ForumPostEdit
                            {
                                EditorAccountID = Global.AccountID,
                                EditTime = DateTime.UtcNow,
                                OriginalText = post.Text,
                                NewText = text
                            });
                        post.Text = text;
                    } else
                    {
                        var p = new ForumPost { AuthorAccountID = Global.AccountID, Text = text, Created = DateTime.UtcNow };
                        thread.ForumPosts.Add(p);
                        db.SaveChanges();
                        gotoPostId = p.ForumPostID;
                    }

                    thread.LastPost = DateTime.UtcNow;
                    thread.LastPostAccountID = Global.AccountID;
                    thread.PostCount = thread.ForumPosts.Count();
                    thread.UpdateLastRead(Global.AccountID, true, thread.LastPost);

                    db.SubmitChanges();
                }
                
                scope.Complete();

                
                if (missionID.HasValue) return RedirectToAction("Detail", "Missions", new { id = missionID });
                if (resourceID.HasValue) return RedirectToAction("Detail", "Maps", new { id = resourceID });
                if (springBattleID.HasValue) return RedirectToAction("Detail", "Battles", new { id = springBattleID });
                if (clanID.HasValue) return RedirectToAction("Detail", "Clans", new { id = clanID });
                if (planetID.HasValue) return RedirectToAction("Planet", "Planetwars", new { id = planetID });
                if (forumPostID.HasValue) return RedirectToAction("Thread","Forum", new { id = thread.ForumThreadID, postID = forumPostID });
                return RedirectToAction("Thread", "Forum", new { id = thread.ForumThreadID, postID = gotoPostId });
            }
        }

        /// <summary>
        ///     Redirects to a thread page given a specified <see cref="ForumPost" /> ID
        /// </summary>
        public ActionResult Post(int id) {
            var db = new ZkDataContext();
            var post = db.ForumPosts.FirstOrDefault(x => x.ForumPostID == id);
            var thread = post.ForumThread;
            return RedirectToAction("Thread", new { id = thread.ForumThreadID, postID= id });
        }

        /// <summary>
        ///     Go to a specific <see cref="ForumThread" />
        /// </summary>
        /// <param name="lastPost">Go to last post</param>
        /// <param name="lastSeen">UNUSED</param>
        /// <param name="postID">A specific <see cref="ForumPost" /> ID to go to</param>
        /// <returns></returns>
        public ActionResult Thread(int id, int? postID) {
            var db = new ZkDataContext();
            var t = db.ForumThreads.FirstOrDefault(x => x.ForumThreadID == id);

            // TODO - indicate thread has been deleted
            if (t == null) return RedirectToAction("Index");


            var cat = t.ForumCategory;
            if (cat != null)
            {
                if (cat.ForumMode == ForumMode.Missions && t.Missions.Any()) return RedirectToAction("Detail", "Missions", new { id = t.Missions.First().MissionID });
                if (cat.ForumMode == ForumMode.Maps && t.Resources.Any()) return RedirectToAction("Detail", "Maps", new { id = t.Resources.First().ResourceID });
                if (cat.ForumMode == ForumMode.SpringBattles && t.SpringBattles.Any()) return RedirectToAction("Detail", "Battles", new { id = t.SpringBattles.First().SpringBattleID });
                if (cat.ForumMode == ForumMode.Clans && t.Clan!=null) return RedirectToAction("Detail", "Clans", new { id = t.RestrictedClanID });
                if (cat.ForumMode == ForumMode.Planets && t.Planets.Any()) return RedirectToAction("Planet", "Planetwars", new { id = t.Planets.First().PlanetID });
            }

            var res = new ThreadResult();
            res.GoToPost = postID ?? t.UpdateLastRead(Global.AccountID, false);

            db.SubmitChanges();

            res.Path = cat?.GetPath() ?? new List<ForumCategory>();
            res.CurrentThread = t;

            return View(res);
        }

        [Auth(Role = AuthRole.ZkAdmin)]
        public ActionResult AdminThread(int threadID, int newcat, bool isPinned, bool isLocked) {
            var db = new ZkDataContext();
            var thread = db.ForumThreads.Single(x => x.ForumThreadID == threadID);
            thread.ForumCategoryID = newcat;
            thread.IsPinned = isPinned;
            thread.IsLocked = isLocked;
            db.SubmitChanges();
            return RedirectToAction("Index", new { categoryID = newcat });
        }


        /// <summary>
        ///     Upvote or downvote a post
        /// </summary>
        /// <param name="delta">+1 or -1</param>
        /// <returns></returns>
        [Auth]
        public ActionResult VotePost(int forumPostID, int delta) {
            var db = new ZkDataContext();
            var myAcc = Global.Account;

            if (myAcc.Level < GlobalConst.MinLevelForForumVote) return Content(string.Format("You cannot vote until you are level {0} or higher", GlobalConst.MinLevelForForumVote));
            if ((Global.Account.ForumTotalUpvotes - Global.Account.ForumTotalDownvotes) < GlobalConst.MinNetKarmaToVote) return Content("Your net karma is too low to vote");

            if (delta > 1) delta = 1;
            else if (delta < -1) delta = -1;

            var post = db.ForumPosts.First(x => x.ForumPostID == forumPostID);
            var author = post.Account;
            if (author.AccountID == Global.AccountID) return Content("Cannot vote for your own posts");
            if (myAcc.VotesAvailable <= 0) return Content("Out of votes");

            var existingVote = db.AccountForumVotes.SingleOrDefault(x => x.ForumPostID == forumPostID && x.AccountID == Global.AccountID);
            if (existingVote != null) // clear existing vote
            {
                var oldDelta = existingVote.Vote;
                // reverse vote effects
                if (oldDelta > 0)
                {
                    author.ForumTotalUpvotes = author.ForumTotalUpvotes - oldDelta;
                    post.Upvotes = post.Upvotes - oldDelta;
                } else if (oldDelta < 0)
                {
                    author.ForumTotalDownvotes = author.ForumTotalDownvotes + oldDelta;
                    post.Downvotes = post.Downvotes + oldDelta;
                }
                db.AccountForumVotes.DeleteOnSubmit(existingVote);
            }
            if (delta > 0)
            {
                author.ForumTotalUpvotes = author.ForumTotalUpvotes + delta;
                post.Upvotes = post.Upvotes + delta;
            } else if (delta < 0)
            {
                author.ForumTotalDownvotes = author.ForumTotalDownvotes - delta;
                post.Downvotes = post.Downvotes - delta;
            }

            if (delta != 0)
            {
                var voteEntry = new AccountForumVote { AccountID = Global.AccountID, ForumPostID = forumPostID, Vote = delta };
                db.AccountForumVotes.InsertOnSubmit(voteEntry);
                myAcc.VotesAvailable--;
            }

            db.SubmitChanges();

            return RedirectToAction("Thread", new { id = post.ForumThreadID, postID = forumPostID });
        }

        /// <summary>
        ///     Removes an existing vote on a post
        /// </summary>
        [Auth]
        public ActionResult CancelVotePost(int forumPostID) {
            var db = new ZkDataContext();
            var existingVote = db.AccountForumVotes.SingleOrDefault(x => x.ForumPostID == forumPostID && x.AccountID == Global.AccountID);
            if (existingVote == null) return Content("No existing vote to remove");

            var post = db.ForumPosts.First(x => x.ForumPostID == forumPostID);
            var author = post.Account;

            var delta = existingVote.Vote;
            // reverse vote effects
            if (delta > 0)
            {
                author.ForumTotalUpvotes = author.ForumTotalUpvotes - delta;
                post.Upvotes = post.Upvotes - delta;
            } else if (delta < 0)
            {
                author.ForumTotalDownvotes = author.ForumTotalDownvotes + delta;
                post.Downvotes = post.Downvotes + delta;
            }
            db.AccountForumVotes.DeleteOnSubmit(existingVote);

            db.SubmitChanges();

            return RedirectToAction("Thread", new { id = post.ForumThreadID, postID = forumPostID });
        }

        public static int GetPostPage(ForumPost post) {
            if (post == null) return 0;
            var index = post.ForumThread.ForumPosts.Count(x => x.ForumPostID < post.ForumPostID);
            return index/PageSize;
        }

        public ActionResult Search() {
            return View("Search");
        }

        public ActionResult SubmitSearch(
            string keywords,
            string username,
            List<int> categoryIDs,
            bool firstPostOnly = false,
            bool resultsAsPosts = true) {
            if (string.IsNullOrEmpty(keywords) && string.IsNullOrEmpty(username)) return Content("You must enter keywords and/or username");

            var db = new ZkDataContext();
            if (categoryIDs == null) categoryIDs = new List<int>();

            var posts =
                db.ForumPosts.Where(
                    x =>
                        (string.IsNullOrEmpty(username) || x.Account.Name == username && !x.Account.IsDeleted) &&
                        (categoryIDs.Count == 0 || categoryIDs.Contains((int)x.ForumThread.ForumCategoryID)) &&
                        (x.ForumThread.RestrictedClanID == null || x.ForumThread.RestrictedClanID == Global.ClanID));
            if (firstPostOnly)
                posts = posts.Where(x => x.ForumThread.ForumPosts.FirstOrDefault() == x);
            posts = Global.ForumPostIndexer.FilterPosts(posts, keywords);

            return View("SearchResults", new SearchResult { Posts = posts.OrderByDescending(x=>x.Created).Take(100).ToList(), DisplayAsPosts = resultsAsPosts });
        }

        /// <summary>
        ///     Marks all threads as read
        /// </summary>
        /// <param name="categoryID">The subforum category ID; will be applied to all subforums if null</param>
        /// <returns></returns>
        /// <remarks>
        ///     Unlike the normal system that tags a thread as read, this one sets a single date value for one or all
        ///     subforums; a thread is read if its last post is older than this date
        /// </remarks>
        [Auth]
        public ActionResult MarkAllAsRead(int? categoryID) {
            var db = new ZkDataContext();
            if (categoryID != null)
            {
                var lastRead = db.ForumLastReads.FirstOrDefault(x => x.AccountID == Global.AccountID && x.ForumCategoryID == categoryID);
                if (lastRead == null)
                {
                    db.ForumLastReads.InsertOnSubmit(
                        new ForumLastRead { AccountID = Global.AccountID, ForumCategoryID = (int)categoryID, LastRead = DateTime.UtcNow });
                } else lastRead.LastRead = DateTime.UtcNow;
            } else
            {
                foreach (var categoryID2 in db.ForumCategories.Select(x => x.ForumCategoryID))
                {
                    var lastRead = db.ForumLastReads.FirstOrDefault(x => x.AccountID == Global.AccountID && x.ForumCategoryID == categoryID2);
                    if (lastRead == null)
                    {
                        db.ForumLastReads.InsertOnSubmit(
                            new ForumLastRead { AccountID = Global.AccountID, ForumCategoryID = categoryID2, LastRead = DateTime.UtcNow });
                    } else lastRead.LastRead = DateTime.UtcNow;
                }
            }
            db.SubmitChanges();
            return RedirectToAction("Index", new { categoryID });
        }

        public class IndexResult
        {
            public IEnumerable<ForumCategory> Categories;
            public ForumCategory CurrentCategory;
            public List<ForumCategory> Path = new List<ForumCategory>();
            public IQueryable<ForumThread> Threads;
            public int? CategoryID { get; set; }
            public string Search { get; set; }
            public bool OnlyUnread { get; set; }
            public string User { get; set; }
        }

        public class NewPostResult
        {
            public bool CanSetTopic;
            public ForumCategory CurrentCategory;
            public ForumThread CurrentThread;
            public ForumPost EditedPost;
            public IEnumerable<ForumPost> LastPosts;
            public List<ForumCategory> Path = new List<ForumCategory>();
            public string WikiKey;
        }

        public class ThreadResult
        {
            public ForumThread CurrentThread;
            public int GoToPost;
            public List<ForumCategory> Path = new List<ForumCategory>();
        }

        public class SearchResult
        {
            public bool DisplayAsPosts;
            public List<ForumPost> Posts;
        }


        [ValidateInput(false)]
        public ActionResult Preview(string text) {
            return View("Preview",(object)text);
        }
    }
}