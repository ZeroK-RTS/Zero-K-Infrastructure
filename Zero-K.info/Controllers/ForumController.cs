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

        void ResetThreadLastPostTime(int threadID)
        {
            var db = new ZkDataContext();
            var thread = db.ForumThreads.FirstOrDefault(x=> x.ForumThreadID == threadID);
            DateTime lastPost = thread.Created;
            foreach (ForumPost p in thread.ForumPosts.Reverse())
            {
                if (p.ForumPostEdits.Count > 0)
                {
                    var lastEdit = p.ForumPostEdits.Last().EditTime;
                    if (lastEdit > lastPost) lastPost = lastEdit;
                }
                else if (p.Created > lastPost) lastPost = p.Created;
            }
            thread.LastPost = lastPost;
            db.SubmitChanges();
        }

		[Auth(Role = AuthRole.ZkAdmin)]
		public ActionResult DeletePost(int? postID)
		{
			var db = new ZkDataContext();
			var post = db.ForumPosts.Single(x => x.ForumPostID == postID);
			var thread = post.ForumThread;
            int threadID = thread.ForumThreadID;
            //int index = post.ForumThread.ForumPosts.IndexOf(post);
            int page = GetPostPage(post);

			db.ForumPosts.DeleteOnSubmit(post);
			if (thread.ForumPosts.Count() <= 1) {
                db.ForumThreadLastReads.DeleteAllOnSubmit(db.ForumThreadLastReads.Where(x => x.ForumThread == thread).ToList());
				db.ForumThreads.DeleteOnSubmit(thread);
				db.SubmitChanges();
				return RedirectToAction("Index");
			}
			db.SubmitChanges();
            ResetThreadLastPostTime(threadID);
			return RedirectToAction("Thread", new { id = threadID, page = page });
		}

        [Auth(Role = AuthRole.ZkAdmin)]
        public ActionResult DeleteAllPostsByUser(int accountID, string accountName)
        {
            var db = new ZkDataContext();
            Account acc = db.Accounts.FirstOrDefault(x => x.AccountID == accountID);
            if (acc.Name != accountName) return Content("Invalid safety code");
            foreach (ForumPost p in acc.ForumPosts) DeletePost(p.ForumPostID);
            return RedirectToAction("Index");
        }

		public ActionResult Index(int? categoryID, int? page = null)
		{
			var db = new ZkDataContext();
			var res = new IndexResult();

			res.Categories = db.ForumCategories.Where(x => x.ParentForumCategoryID == categoryID).OrderBy(x => x.SortOrder);

			res.Path = GetCategoryPath(categoryID, db);
			res.CurrentCategory = res.Path.LastOrDefault();

            var threads = db.ForumThreads.Where(x => x.ForumCategoryID == categoryID).OrderByDescending(x => x.IsPinned).ThenByDescending(x => x.LastPost);
            res.Page = page ?? 0;
            res.PageCount = ((threads.Count() - 1) / PageSize) + 1;
            res.Threads = threads.Skip((page ?? 0) * PageSize).Take(PageSize).ToList();

			return View(res);
		}

        [Auth]
		public ActionResult NewPost(int? categoryID, int? threadID, int? forumPostID)
		{
			var res = new NewPostResult();
			var db = new ZkDataContext();

            var penalty = ZkData.Punishment.GetActivePunishment(Global.AccountID, "", 0, x => x.BanForum);
            if (penalty != null)
            {
                return Content(string.Format("You cannot post while banned from forum!\nExpires: {0} UTC\nReason: {1}", penalty.BanExpires, penalty.Reason));
            }

            var clan = db.Clans.FirstOrDefault(x => x.ForumThreadID == threadID);
            if (clan != null && Global.ClanID != clan.ClanID)
            {
                return Content(string.Format("You are not a member of {0}, you cannot post in their clan thread", clan.ClanName));
            }

			if (threadID.HasValue)
			{
				var t = db.ForumThreads.Single(x => x.ForumThreadID == threadID.Value);
				res.CurrentThread = t;
				res.LastPosts = res.CurrentThread.ForumPosts.OrderByDescending(x => x.ForumPostID).Take(20);
				if (!categoryID.HasValue) categoryID = t.ForumCategoryID;
			}

			res.Path = GetCategoryPath(categoryID, db);
            var category = res.Path.LastOrDefault();
			res.CurrentCategory = category;
            if (forumPostID != null) {
                ForumPost post = db.ForumPosts.Single(x=>x.ForumPostID == forumPostID);
                if (!Global.IsZeroKAdmin && Global.AccountID != post.AuthorAccountID)
                {
                    return Content("You cannot edit this post");
                }
                res.EditedPost= post;   
            }
            if (threadID != null)
            {
                var thread = res.CurrentThread;
                res.CanSetTopic = (thread.ForumPosts.Count > 0 && thread.ForumPosts.First().ForumPostID == forumPostID 
                    && !category.IsClans && !category.IsMaps && !category.IsMissions && !category.IsPlanets && !category.IsSpringBattles);
            }
            else res.CanSetTopic = true;

			return View(res);
		}

		[Auth]
		public ActionResult SubmitPost(int? threadID, int? categoryID, int? resourceID, int? missionID, int? springBattleID, int? clanID, int? planetID, string text, string title, int? forumPostID)
		{
            if (threadID == null && missionID == null && resourceID == null && springBattleID == null && clanID ==null && planetID == null && forumPostID==null && string.IsNullOrWhiteSpace(title)) return Content("Cannot post new thread with blank title");
			if (string.IsNullOrWhiteSpace(text)) return Content("Please type some text :)");

            var penalty = ZkData.Punishment.GetActivePunishment(Global.AccountID, "", 0, x => x.BanForum);
            if (penalty != null)
            {
                return Content(string.Format("You cannot post while banned from forum!\nExpires: {0} UTC\nReason: {1}", penalty.BanExpires, penalty.Reason));
            }

			var db = new ZkDataContext();
			using (var scope = new TransactionScope())
			{
				var thread = db.ForumThreads.SingleOrDefault(x => x.ForumThreadID == threadID);

				// update title
                if (thread != null && !String.IsNullOrEmpty(title)) thread.Title = title;
				if (thread != null && planetID != null)
				{
					var planet = db.Planets.Single(x => x.PlanetID == planetID);
					thread.Title = "Planet "  + planet.Name;
				}
				if (thread != null && clanID != null)
				{
					var clan = db.Clans.Single(x => x.ClanID == clanID);
					thread.Title = "Clan " + clan.ClanName;
				}
				if (thread != null && missionID != null)
				{
					var mission = db.Missions.Single(x => x.MissionID == missionID);
					thread.Title = "Mission " +mission.Name;
				}
                if (thread != null && resourceID != null) {
                    var map = db.Resources.Single(x => x.ResourceID == resourceID);
                    thread.Title = "Map " + map.InternalName;
                }


			    if (threadID == null && categoryID.HasValue) // new thread
				{
					var cat = db.ForumCategories.Single(x => x.ForumCategoryID == categoryID.Value);
					if (cat.IsLocked) return Content("Thread is locked");

					if (string.IsNullOrEmpty(title)) return Content("Title cannot be empty");
					thread = new ForumThread();
					thread.CreatedAccountID = Global.AccountID;
					thread.Title = title;
					thread.ForumCategoryID = cat.ForumCategoryID;
					db.ForumThreads.InsertOnSubmit(thread);
				}

				if (thread == null && resourceID != null) // non existing thread, we posted new post on map
				{
					var res = db.Resources.Single(x => x.ResourceID == resourceID);
                    if (res.ForumThread != null) return Content("Double post");
                    thread = new ForumThread() { Title = "Map " +res.InternalName, CreatedAccountID = Global.AccountID, LastPostAccountID = Global.AccountID };
					thread.ForumCategory = db.ForumCategories.FirstOrDefault(x => x.IsMaps);
					res.ForumThread = thread;
					db.ForumThreads.InsertOnSubmit(thread);
				}

				if (thread == null && springBattleID != null) // non existing thread, we posted new post on battle
				{
					var bat = db.SpringBattles.Single(x => x.SpringBattleID == springBattleID);
                    if (bat.ForumThread != null) return Content("Double post");
                    thread = new ForumThread() { Title =  bat.FullTitle, CreatedAccountID = Global.AccountID, LastPostAccountID = Global.AccountID };
					thread.ForumCategory = db.ForumCategories.FirstOrDefault(x => x.IsSpringBattles);
				    bat.ForumThread = thread;
					db.ForumThreads.InsertOnSubmit(thread);
				}

				if (thread == null && clanID != null)
				{
					var clan = db.Clans.Single(x => x.ClanID == clanID);
                    if (clan.ForumThread != null) return Content("Double post");
                    thread = new ForumThread() { Title = "Clan " +clan.ClanName, CreatedAccountID = Global.AccountID, LastPostAccountID = Global.AccountID };
					thread.ForumCategory = db.ForumCategories.FirstOrDefault(x => x.IsClans);
					clan.ForumThread = thread;
					thread.Clan = clan;
					db.ForumThreads.InsertOnSubmit(thread);
				}

				if (thread == null && planetID != null)
				{
					var planet = db.Planets.Single(x => x.PlanetID == planetID);
                    if (planet.ForumThread != null) return Content("Double post");
                    thread = new ForumThread() { Title = "Planet " +planet.Name, CreatedAccountID = Global.AccountID, LastPostAccountID = Global.AccountID };
					thread.ForumCategory = db.ForumCategories.FirstOrDefault(x => x.IsPlanets);
					planet.ForumThread = thread;
					db.ForumThreads.InsertOnSubmit(thread);
				}

				if (thread == null) return Content("Thread not found");
				if (thread.IsLocked) return Content("Thread is locked");
                
				var lastPost = thread.ForumPosts.OrderByDescending(x => x.ForumPostID).FirstOrDefault();

                //double post preventer
                if (lastPost == null || lastPost.AuthorAccountID != Global.AccountID || lastPost.Text != text)
				{
                    if (forumPostID != null) {
                        var post = thread.ForumPosts.Single(x => x.ForumPostID == forumPostID);
                        if (post.AuthorAccountID != Global.AccountID && !Global.Account.IsZeroKAdmin) throw new ApplicationException("Not authorized to edit the post");
                        post.ForumPostEdits.Add(new ForumPostEdit() { 
                                EditorAccountID = Global.AccountID,
                                EditTime = DateTime.UtcNow,
                                OriginalText = post.Text,
                                NewText = text
                        });
                        post.Text = text;


                    } else thread.ForumPosts.Add(new ForumPost() { AuthorAccountID = Global.AccountID, Text = text, Created = DateTime.UtcNow});

					
                    thread.LastPost = DateTime.UtcNow;
					thread.LastPostAccountID = Global.AccountID;
					thread.PostCount = thread.ForumPosts.Count();
					thread.UpdateLastRead(Global.AccountID, true, thread.LastPost);

					db.SubmitChanges();
				}
                int lastPage = ((thread.PostCount - 1) / PageSize);
                scope.Complete();

				if (missionID.HasValue) return RedirectToAction("Detail", "Missions", new { id = missionID });
				else if (resourceID.HasValue) return RedirectToAction("Detail", "Maps", new { id = resourceID });
				else if (springBattleID.HasValue) return RedirectToAction("Detail", "Battles", new { id = springBattleID });
				else if (clanID.HasValue) return RedirectToAction("Detail", "Clans", new { id = clanID });
				else if (planetID.HasValue) return RedirectToAction("Planet", "Planetwars", new { id = planetID });
                else if (forumPostID.HasValue) return RedirectToAction("Thread", new { id = thread.ForumThreadID, postID = forumPostID });
                else return RedirectToAction("Thread", new { id = thread.ForumThreadID, page = lastPage});
			}
		}

        public ActionResult Post(int id)
        {
            var db = new ZkDataContext();
            ForumPost post = db.ForumPosts.FirstOrDefault(x => x.ForumPostID == id);
            int? page = GetPostPage(post);
            if (page == 0) page = null;
            ForumThread thread = post.ForumThread;
            return RedirectToAction("Thread", new { id = thread.ForumThreadID, page = page});

        }

		public ActionResult Thread(int id, bool? lastPost, bool? lastSeen, int? postID, int? page)
		{
			var db = new ZkDataContext();
			var t = db.ForumThreads.FirstOrDefault(x => x.ForumThreadID == id);

            // TODO - indicate thread has been deleted
            if (t == null) return RedirectToAction("Index");

		    if (page == null) {
		        if (postID == null) page = 0;
		        else {
		            var post = t.ForumPosts.FirstOrDefault(x => x.ForumPostID == postID);
		            page = GetPostPage(post);
		        }
		    }
		

			var cat = t.ForumCategory;
			if (cat != null)
			{
				if (cat.IsMissions) return RedirectToAction("Detail", "Missions", new { id = t.Missions.First().MissionID });
				if (cat.IsMaps) return RedirectToAction("Detail", "Maps", new { id = t.Resources.First().ResourceID });
				if (cat.IsSpringBattles) return RedirectToAction("Detail", "Battles", new { id = t.SpringBattles.First().SpringBattleID });
				if (cat.IsClans) return RedirectToAction("Detail", "Clans", new { id = t.RestrictedClanID});
				if (cat.IsPlanets) return RedirectToAction("Planet", "Planetwars", new { id = t.Planets.First().PlanetID});
			}

			var res = new ThreadResult();
			res.GoToPost = postID ?? t.UpdateLastRead(Global.AccountID, false);

			db.SubmitChanges();

			res.Path = GetCategoryPath(t.ForumCategoryID, db);
			res.CurrentThread = t;
			res.PageCount = ((t.PostCount-1)/PageSize) + 1;
            res.Page = page;
			res.Posts = t.ForumPosts.AsQueryable().Skip((page ?? 0)*PageSize).Take(PageSize).ToList();

			return View(res);
		}

		static IEnumerable<ForumCategory> GetCategoryPath(int? categoryID, ZkDataContext db)
		{
			var path = new List<ForumCategory>();
			var id = categoryID;
			while (id != null)
			{
				var cat = db.ForumCategories.SingleOrDefault(x => x.ForumCategoryID == id);
				path.Add(cat);
				id = cat.ParentForumCategoryID;
			}
			path.Reverse();
			return path;
		}

		public class IndexResult
		{
			public IEnumerable<ForumCategory> Categories;
			public ForumCategory CurrentCategory;
            public int? Page;
            public int PageCount;
			public IEnumerable<ForumCategory> Path;
			public IEnumerable<ForumThread> Threads;
		}

		public class NewPostResult
		{
			public ForumCategory CurrentCategory;
			public ForumThread CurrentThread;
			public IEnumerable<ForumPost> LastPosts;
			public IEnumerable<ForumCategory> Path;
            public ForumPost EditedPost;
            public bool CanSetTopic;
		}

		public class ThreadResult
		{
			public ForumThread CurrentThread;
			public int GoToPost;
            public int? Page;
			public int PageCount;
			public IEnumerable<ForumCategory> Path;
			public List<ForumPost> Posts;
		}

        public class SearchResult
        {
            public List<ForumPost> Posts;
            public bool DisplayAsPosts;
        }

        [Auth(Role = AuthRole.ZkAdmin)]
		public ActionResult AdminThread(int threadID, int newcat, bool isPinned, bool isLocked)
		{
			var db = new ZkDataContext();
			var thread = db.ForumThreads.Single(x => x.ForumThreadID == threadID);
			thread.ForumCategoryID = newcat;
		    thread.IsPinned = isPinned;
		    thread.IsLocked = isLocked;
			db.SubmitChanges();
			return RedirectToAction("Index", new { categoryID = newcat });
		}

	    public ActionResult EditHistory(int forumPostID)
	    {
            var db = new ZkDataContext();
            var post = db.ForumPosts.First(x => x.ForumPostID == forumPostID);
            return View("EditHistory", post);
	    }

        [Auth]
        public ActionResult VotePost(int forumPostID, int delta)
        {
            var db = new ZkDataContext();
            Account myAcc = Global.Account;

            if (myAcc.Level < GlobalConst.MinLevelForForumVote)
            {
                return Content(string.Format("You cannot vote until you are level {0} or higher", GlobalConst.MinLevelForForumVote));
            }
            if ((Global.Account.ForumTotalUpvotes - Global.Account.ForumTotalDownvotes) < GlobalConst.MinNetKarmaToVote)
            {
                return Content("Your net karma is too low to vote");
            }

            if (delta > 1) delta = 1;
            else if (delta < -1) delta = -1;

            ForumPost post = db.ForumPosts.First(x => x.ForumPostID == forumPostID);
            Account author = post.Account;
            if (author.AccountID == Global.AccountID) return Content("Cannot vote for your own posts");
            if (myAcc.VotesAvailable <= 0) return Content("Out of votes");

            AccountForumVote existingVote = db.AccountForumVotes.SingleOrDefault(x => x.ForumPostID == forumPostID && x.AccountID == Global.AccountID);
            if (existingVote != null)   // clear existing vote
            {
                int oldDelta = existingVote.Vote;
                // reverse vote effects
                if (oldDelta > 0)
                {
                    author.ForumTotalUpvotes = author.ForumTotalUpvotes - oldDelta;
                    post.Upvotes = post.Upvotes - oldDelta;
                }
                else if (oldDelta < 0)
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
            }
            else if (delta < 0)
            {
                author.ForumTotalDownvotes = author.ForumTotalDownvotes - delta;
                post.Downvotes = post.Downvotes - delta;
            }

            if (delta != 0) {
                AccountForumVote voteEntry = new AccountForumVote { AccountID = Global.AccountID, ForumPostID = forumPostID, Vote = delta };
                db.AccountForumVotes.InsertOnSubmit(voteEntry);
                myAcc.VotesAvailable--;
            }

            db.SubmitChanges();

            return RedirectToAction("Thread", new { id = post.ForumThreadID, postID = forumPostID});
        }

        [Auth]
        public ActionResult CancelVotePost(int forumPostID)
        {
            var db = new ZkDataContext();
            AccountForumVote existingVote = db.AccountForumVotes.SingleOrDefault(x => x.ForumPostID == forumPostID && x.AccountID == Global.AccountID);
            if (existingVote == null) return Content("No existing vote to remove");

            ForumPost post = db.ForumPosts.First(x => x.ForumPostID == forumPostID);
            Account author = post.Account;

            int delta = existingVote.Vote;
            // reverse vote effects
            if (delta > 0)
            {
                author.ForumTotalUpvotes = author.ForumTotalUpvotes - delta;
                post.Upvotes = post.Upvotes - delta;
            }
            else if (delta < 0)
            {
                author.ForumTotalDownvotes = author.ForumTotalDownvotes + delta;
                post.Downvotes = post.Downvotes + delta;
            }
            db.AccountForumVotes.DeleteOnSubmit(existingVote);

            db.SubmitChanges();

            return RedirectToAction("Thread", new { id = post.ForumThreadID, postID = forumPostID });
        }

        public static int GetPostPage(ForumPost post)
        {
            if (post == null) return 0;
            var index = post.ForumThread.ForumPosts.Count(x=>x.ForumPostID < post.ForumPostID);
            return index / PageSize;
        }

        public ActionResult Search()
        {
            return View("Search");
        }

        public ActionResult SubmitSearch(string keywords, string username, List<int> categoryIDs, bool firstPostOnly = false, bool resultsAsPosts = true)
        {
            if (String.IsNullOrEmpty(keywords) && String.IsNullOrEmpty(username)) return Content("You must enter keywords and/or username");
            
            ZkDataContext db = new ZkDataContext();
            if (categoryIDs == null) categoryIDs = new List<int>();

            var posts = db.ForumPosts.Where(x=> (String.IsNullOrEmpty(username) || x.Account.Name == username)
                && (categoryIDs.Count == 0 || categoryIDs.Contains((int)x.ForumThread.ForumCategoryID))
                && (x.ForumThread.RestrictedClanID == null || x.ForumThread.RestrictedClanID == Global.ClanID)
                ).OrderByDescending(x=> x.Created).ToList();
            if (firstPostOnly) posts = posts.Where(x => x.ForumThread.ForumPosts.First() == x).ToList();
            var invalidResults = new List<ForumPost>();
            if (!String.IsNullOrEmpty(keywords))
            {
                string[] keywordArray = keywords.Split(null as string[], StringSplitOptions.RemoveEmptyEntries);
                foreach (ForumPost p in posts)
                {
                    /*  // use this for an OR search
                    bool success = false;
                    foreach (string word in keywordArray)
                    {
                        if (p.Text.Contains(word))
                        {
                            success = true;
                            break;
                        }
                    }
                    if (!success) invalidResults.Add(p);
                    */
                    // AND search
                    foreach (string word in keywordArray)
                    {
                        if (!p.Text.Contains(word))
                        {
                            invalidResults.Add(p);
                        }
                    }
                }
            }
            posts = posts.Where(x => !invalidResults.Contains(x)).ToList();
            return View("SearchResults", new SearchResult {Posts = posts.Take(100).ToList(), DisplayAsPosts = resultsAsPosts});
        }

        [Auth]
        public ActionResult MarkAllAsRead(int? categoryID)
        {
            ZkDataContext db = new ZkDataContext();
            if (categoryID != null)
            {
                var lastRead = db.ForumLastReads.FirstOrDefault(x => x.AccountID == Global.AccountID && x.ForumCategoryID == categoryID);
                if (lastRead == null) db.ForumLastReads.InsertOnSubmit(new ForumLastRead { AccountID = Global.AccountID, ForumCategoryID = (int)categoryID, LastRead = DateTime.UtcNow });
                else lastRead.LastRead = DateTime.UtcNow;
            }
            else
            {
                foreach (int categoryID2 in db.ForumCategories.Select(x=> x.ForumCategoryID))
                {
                    var lastRead = db.ForumLastReads.FirstOrDefault(x => x.AccountID == Global.AccountID && x.ForumCategoryID == categoryID2);
                    if (lastRead == null) db.ForumLastReads.InsertOnSubmit(new ForumLastRead { AccountID = Global.AccountID, ForumCategoryID = categoryID2, LastRead = DateTime.UtcNow });
                    else lastRead.LastRead = DateTime.UtcNow;
                }
            }
            db.SubmitChanges();
            return RedirectToAction("Index", new { categoryID = categoryID });
        }
	}
}