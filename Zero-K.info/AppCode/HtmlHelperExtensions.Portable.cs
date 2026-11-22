using System;
using System.Linq;   // SingleOrDefault over a navigation collection
using System.Collections.Generic;   // IEnumerable<SelectListItem> from GetFactionItems
using System.Linq.Expressions;      // Expression<Func<>> in the filter it takes
using System.IO;                    // File.Exists in IncludeFile
using System.Net;                   // WebClient in IncludeWiki
using ZeroKWeb;   // Global.AccountID, shimmed in the port by Mvc5Compat/GlobalCompat.cs
using ZkData;     // Account, ZkDataContext

namespace System.Web.Mvc
{
    // The half of HtmlHelperExtensions.cs that has nothing to do with MVC 5, split out so the
    // .NET 9 projects can link it - the same move already made for GlobalConst, Utils and
    // RatingSystems.
    //
    // The namespace stays System.Web.Mvc, which looks wrong in a file that no longer mentions
    // MVC and is the entire point: MVC 5 views see these without a using because Views/Web.config
    // imports that namespace, and changing it would change how every one of those views resolves
    // names. A namespace is just a string; keeping it costs nothing and keeps the Framework
    // build byte-identical. The .NET 9 side imports it explicitly in _ViewImports.cshtml.

    public enum StarType
    {
        RedStarSmall,
        GreenStarSmall,
        WhiteStarSmall,
        RedSkull,
        WhiteSkull
    }

    public static partial class HtmlHelperExtensions
    {
        public static string ToAgoString(this DateTime? utcDate) {
            if (utcDate.HasValue) return ToAgoString(DateTime.UtcNow.Subtract(utcDate.Value));
            else return "";
        }

        public static string ToAgoString(this DateTime utcDate) {
            return ToAgoString(DateTime.UtcNow.Subtract(utcDate));
        }

        public static string ToAgoString(this TimeSpan timeSpan) {
            if (timeSpan.TotalSeconds > 0) return string.Format("{0} ago", timeSpan.Duration().ToNiceString());
            else return string.Format("in {0}", timeSpan.Duration().ToNiceString());
        }

        public static string ToNiceString(this TimeSpan timeSpan) {
            if (timeSpan.TotalMinutes < 2) return string.Format("{0} seconds", (int)timeSpan.TotalSeconds);
            if (timeSpan.TotalHours < 2) return string.Format("{0} minutes", (int)timeSpan.TotalMinutes);
            if (timeSpan.TotalDays < 2) return string.Format("{0} hours", (int)timeSpan.TotalHours);
            if (timeSpan.TotalDays < 60) return string.Format("{0} days", (int)timeSpan.TotalDays);
            if (timeSpan.TotalDays < 365*2) return string.Format("{0} months", (int)(timeSpan.TotalDays / 30));
            return string.Format("{0} years", (int)(timeSpan.TotalDays/365));
        }
    
        /// <summary>
        /// The signed-in account, from a context the caller already has. Pure ZkData and
        /// Global, both of which the port has - it was in the MVC-dependent half only
        /// because that is where it was written. Planet.cshtml reads it twice.
        /// </summary>
        public static Account CurrentAccount(this ZkDataContext db)
        {
            if (Global.AccountID > 0 && Global.IsAccountAuthorized) return db.Accounts.Find(Global.AccountID);
            else return null;
        }

        // Moved here from HtmlHelperExtensions.cs so the port can link them: these are what
        // Views/Shared/DisplayTemplates/ForumPost.cshtml needs, and that template is what
        // renders the text of every forum post. Nothing in them is MVC 5 specific - MvcHtmlString
        // is shimmed and HtmlHelper is aliased to IHtmlHelper - so this is the same relocation
        // made for PwPhase, the GlobalConst members, CurrentAccount and PrintTimeRemaining.
        public static MvcHtmlString BBCodeCached(this HtmlHelper helper, ForumPost post) {
            return Global.ForumPostCache.GetCachedHtml(post, helper);
        }

        public static MvcHtmlString BBCodeCached(this HtmlHelper helper, News news)
        {
            return Global.ForumPostCache.GetCachedHtml(news, helper);
        }

        /// <summary>
        ///     <para>Returns the sum of the + and - votes on the specified <see cref="ForumPost"/></para>
        ///     <para>The + and - numbers serve as links to vote on the post</para>
        ///     <para>Also includes a link to cancel an existing vote</para>
        ///     <para>The tooltip displays the people who voted for each option</para>
        /// </summary>
        /// <summary>
        /// The opening tag of a vote control: an anchor that POSTs through ZkVote instead of
        /// navigating.
        ///
        /// These were plain &lt;a href&gt;, so a crafted link voted on the visitor's behalf. They
        /// cannot become forms - the widget renders inside PostList's Ajax.BeginForm, which is a
        /// real filter form with its own inputs, and a form cannot nest inside another - so the
        /// vote goes out as an XHR carrying the page's anti-forgery token.
        ///
        /// The url is attribute-encoded because it holds an ampersand between its two route
        /// values; the old version wrote it raw into href='...'.
        /// </summary>
        private static string VoteControl(string action)
        {
            return string.Format("a href='#' onclick='return ZkVote(\"{0}\")'",
                                 WebUtility.HtmlEncode(action));
        }

        /// <param name="blockPost">Removes the vote links; is true if the viewer's <see cref="Account"/> is banned or has too many net downvotes</param>
        public static MvcHtmlString PrintPostRating(this HtmlHelper helper, ForumPost post, bool blockPost = false) {
            var url = Global.UrlHelper();
            bool noLink = (Global.Account == null || Global.AccountID == post.AuthorAccountID || Global.Account.Level < GlobalConst.MinLevelForForumVote || Global.Account.VotesAvailable <= 0 || blockPost);
            AccountForumVote previousVote = post.AccountForumVotes.SingleOrDefault(x => x.AccountID == Global.AccountID);
            bool upvoted = (previousVote != null && previousVote.Vote > 0);
            bool downvoted = (previousVote != null && previousVote.Vote < 0);
            bool votersVisible = (!GlobalConst.OnlyAdminsSeePostVoters || (Global.Account?.AdminLevel >= AdminLevel.Moderator));
            /*
            return new MvcHtmlString(string.Format("<input type='' name='upvote' value='{3}{0}{4}' title='Upvote'> / <input type='submit' name='downvote' value='{5}{1}{6}'> {2}",
                    string.Format("<font {0}>+{1}</font>", post.Upvotes > 0 ? "color='LawnGreen'" : "", post.Upvotes),
                    string.Format("<font {0}>-{1}</font>", post.Downvotes > 0 ? "color='Tomato'" : "", post.Downvotes),
                    previousVote != null ? string.Format("(<input type='submit' name='clearvote' value='clear'>)") : "",
                    upvoted ? "<strong>" : "",
                    upvoted ? "</strong>" : "",
                    downvoted ? "<strong>" : "",
                    downvoted ? "</strong>" : ""));
            */

            string upvote = string.Format("<{0} nicetitle='{1}'>{2}{3}{4}{5}",
                !noLink? VoteControl(url.Action("VotePost", "Forum", new { forumPostID = post.ForumPostID, delta = 1 })) : "span",
                votersVisible? string.Format("$forumVotes${0}", post.ForumPostID) : "Upvote",
                upvoted ? "<strong>" : "",
                string.Format("<font {0}>+{1}</font>", post.Upvotes > 0 ? "color='LawnGreen'" : "", post.Upvotes),
                upvoted ? "</strong>" : "",
                !noLink? "</a>" : "</span>"
            );
            string downvote = string.Format("<{0} nicetitle='{1}'>{2}{3}{4}{5}",
                !noLink? VoteControl(url.Action("VotePost", "Forum", new { forumPostID = post.ForumPostID, delta = -1 })) : "span",
                votersVisible? string.Format("$forumVotes${0}", post.ForumPostID) : "Downvote",
                downvoted ? "<strong>" : "",
                string.Format("<font {0}>-{1}</font>", post.Downvotes > 0 ? "color='Tomato'" : "", post.Downvotes),
                downvoted ? "</strong>" : "",
                !noLink? "</a>" : "</span>"
            );

            return new MvcHtmlString(string.Format("{0} / {1} {2}",
                    upvote,
                    downvote,
                    previousVote != null ? string.Format("(<{0}>cancel</a>)", VoteControl(url.Action("CancelVotePost", "Forum", new { forumPostID = post.ForumPostID }))) : ""
                    ));
        }

        // The view helpers 28 views were waiting on, moved rather than ported: MvcHtmlString is
        // shimmed and HtmlHelper is aliased to IHtmlHelper, so nothing in them is MVC 5 specific.
        // The compiler checks both stacks, which is why this is safe in a way transcribing them
        // would not have been.
        public static MvcHtmlString IncludeWiki(this HtmlHelper helper, string node) {
            var post = new ZkDataContext().ForumThreads.FirstOrDefault(x => x.WikiKey == node)?.ForumPosts.OrderBy(x => x.ForumPostID).FirstOrDefault();
            if (post == null) return null;
            return Global.ForumPostCache.GetCachedHtml(post, helper);
        }

        public static MvcHtmlString PrintSpringLink(this HtmlHelper helper, string link) {
           return new MvcHtmlString(string.Format("javascript:SendLobbyCommand('{0}');void(0);",link));
        }

        /// <summary>
        ///     <para>Returns an appropriately formatted link with thread title and mail icon for a thread</para>
        ///     <para>(e.g. bold = posted in; italics = new; grey icon = already read)</para>
        /// </summary>
        /// <param name="thread">The thread to print</param>
        /// <returns></returns>
        public static MvcHtmlString Print(this HtmlHelper helper, ForumThread thread) {
            var url = Global.UrlHelper();

            ForumThreadLastRead lastRead = null;
            ForumLastRead lastReadForum = null;
            DateTime? lastTime = null;
            if (Global.Account != null)
            {
                lastRead = Global.Account.ForumThreadLastReads.FirstOrDefault(x => x.ForumThreadID == thread.ForumThreadID);
                lastReadForum = Global.Account.ForumLastReads.FirstOrDefault(x => x.ForumCategoryID == thread.ForumCategoryID);
                if (lastReadForum != null) lastTime = lastReadForum.LastRead;
            }
            if (lastRead != null && (lastTime == null || lastRead.LastRead > lastTime)) lastTime = lastRead.LastRead;
            ForumPost post = null;
            if (lastTime != null) post = thread.ForumPosts.FirstOrDefault(x => x.Created > lastTime);
            int page = post != null ? ZeroKWeb.Controllers.ForumController.GetPostPage(post) : (thread.PostCount-1)/GlobalConst.ForumPostsPerPage;

            string link;
            if (page > 0) link = url.Action("Thread", "Forum", new { id = thread.ForumThreadID, page = page});
            else link = url.Action("Thread", "Forum", new { id = thread.ForumThreadID });
            link = string.Format("<a href='{0}' title='$thread${1}' style='word-break:break-all;'>", link, thread.ForumThreadID);

            string format;

            if (lastTime == null) format = "<span>{0}<img src='/img/mail/mail-unread.png' height='15' /><i>{1}</i></a></span>";
            else {
                if (lastTime >= thread.LastPost) format = "<span>{0}<img src='/img/mail/mail-read.png' height='15' />{1}</a></span>";
                else {
                    if (lastRead != null && lastRead.LastPosted != null) format = "<span>{0}<img src='/img/mail/mail-new.png' height='15' /><b>{1}</b></a></span>";
                    else format = "<span>{0}<img src='/img/mail/mail-unread.png' height='15' />{1}</a></span>";
                }
            }

            string title = HttpUtility.HtmlEncode(thread.Title);
            if (!string.IsNullOrEmpty(thread.WikiKey))
            {
                title = string.Format("<span style='color:lightblue'>[{0}]</span> {1}", thread.WikiKey, title);
            }

            return new MvcHtmlString(string.Format(format, link, title));
        }

        /// <summary>
        /// Returns the printed <see cref="Account"/>s that hold specified <see cref="RoleType"/> in the <see cref="Faction"/>
        /// </summary>
        /// <param name="rt">The <see cref="RoleType"/> whose holders should be printed</param>
        /// <param name="f">The <see cref="Faction"/> whose role holders should be printed</param>
        public static MvcHtmlString PrintFactionRoleHolders(this HtmlHelper helper, RoleType rt, Faction f) {
            List<MvcHtmlString> holders = new List<MvcHtmlString>();
            foreach (AccountRole acc in rt.AccountRoles.Where(x=>x.Account.FactionID == f.FactionID)) 
            {
                holders.Add(PrintAccount(helper, acc.Account));
            }
            return new MvcHtmlString(String.Join(", ", holders));
        }

        /// <summary>
        /// Returns the printed <see cref="Account"/>s that hold specified <see cref="RoleType"/> in the <see cref="Clan"/>
        /// </summary>
        /// <param name="rt">The <see cref="RoleType"/> whose holders should be printed</param>
        /// <param name="c">The <see cref="Clan"/> whose role holders should be printed</param>
        public static MvcHtmlString PrintClanRoleHolders(this HtmlHelper helper, RoleType rt, Clan c)
        {
            List<MvcHtmlString> holders = new List<MvcHtmlString>();
            foreach (AccountRole acc in rt.AccountRoles.Where(x => x.Account.ClanID == c.ClanID))
            {
                holders.Add(PrintAccount(helper, acc.Account));
            }
            return new MvcHtmlString(String.Join(", ", holders));
        }

        public static MvcHtmlString IncludeFile(this HtmlHelper helper, string name) {
            if (name.StartsWith("http://") || name.StartsWith("https://")) {
                var ret = new WebClient().DownloadString(name);
                return new MvcHtmlString(ret);
            }
            else {
                var path = Global.MapPath(name);
                return new MvcHtmlString(File.ReadAllText(path));
            }
        }

        public static IEnumerable<SelectListItem> GetFactionItems(this HtmlHelper html, int factionID, Expression<Func<Faction, bool>> filter = null) {
            var ret = new ZkDataContext().Factions.AsQueryable().Where(x => !x.IsDeleted);
            if (filter != null) ret = ret.Where(filter);
            return ret.Select(x => new SelectListItem { Text = x.Name, Value = x.FactionID.ToString(), Selected = x.FactionID == factionID });
        }

        public static MvcHtmlString PrintSeconds(this HtmlHelper helper, int? seconds)
        {
            if (seconds != null) return new MvcHtmlString($"<span nicetitle=\"{seconds}\">{TimeSpan.FromSeconds(seconds.Value).ToNiceString()}</span>");
            else return new MvcHtmlString("");
        }

        public static MvcHtmlString PrintRankProgress(this HtmlHelper helper, Account account)
        {
            var ratio =  Ratings.Ranks.GetRankProgress(account);
            int percentage = (int)Math.Round(ratio * 100);
            var progressText = string.Format("Progress to the next rank: {0}%", percentage);
            if (percentage >= 100)
            {
                if (Ratings.Ranks.ValidateRank(account.Rank + 1))
                {
                    progressText = "Rank up on next victory!";
                }
                else if (Global.IsAccountAuthorized && Global.AccountID == account.AccountID)
                {
                    progressText = "Congratulations, you are officially the best Zero-K player!";
                }
                else
                {
                    progressText = account.Name + " is officially the best Zero-K player.";
                }
            }
            var str = new MvcHtmlString(string.Format("Current rank: <img src='/img/ranks/{0}_{1}.png'  class='icon16' alt='rank' /> {2} <br /> <br /> {3}<br /> <br />Win more games to improve your rank!", account.GetIconLevel(), account.Rank, Ratings.Ranks.RankNames[account.Rank], progressText));
            return str;
        }

        /// <summary>
        /// Returns the sum of the + and - votes on all the specified <see cref="Account"/>'s forum posts
        /// </summary>
        public static MvcHtmlString PrintTotalPostRating(this HtmlHelper helper, Account account)
        {
            return new MvcHtmlString(string.Format("{0} / {1}",
                    string.Format("<font color='LawnGreen'>+{0}</font>", account.ForumTotalUpvotes),
                    string.Format("<font color='Tomato'>-{0}</font>", account.ForumTotalDownvotes)
                    ));
        }

        /// <summary>
        /// Returns a colored string that says whether the specified <see cref="PlanetStructure"/> is ACTIVE, DISABLED or POWERING
        /// </summary>
        /// <param name="s">The <see cref="PlanetStructure"/> whose status should be printed</param>
        public static MvcHtmlString PrintStructureState(this HtmlHelper helper, PlanetStructure s) {
            var url = Global.UrlHelper();
            var state = "";
            if (!s.IsActive) {
                if (s.ActivationTurnCounter == null) state = "<span style='color:red'>DISABLED</span>";
                if (s.ActivationTurnCounter >= 0) {
                    state = string.Format(" <span style='color:orange'>POWERING {0} turns left</span>", (s.TurnsToActivateOverride ?? s.StructureType.TurnsToActivate) - s.ActivationTurnCounter);
                }
            }
            else state = "<span style='color:green'>ACTIVE</span>";
            return new MvcHtmlString(state);
        }
}
}
