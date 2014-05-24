using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Serialization;
using LobbyClient;
using NightWatch;
using PlasmaShared;
using PlasmaShared.UnitSyncLib;
using ZkData;
using Encoder = System.Drawing.Imaging.Encoder;

namespace Fixer
{
    public static class ForumFixer
    {
        public static void GetForumKarmaLadder()
        {
            ZkDataContext db = new ZkDataContext();
            var accounts = db.Accounts.Where(x => x.ForumTotalUpvotes > 0 || x.ForumTotalDownvotes > 0).OrderByDescending(x => x.ForumTotalUpvotes - x.ForumTotalDownvotes).ToList();

            foreach (Account ac in accounts)
            {
                System.Console.WriteLine(ac.Name + "\t+" + ac.ForumTotalUpvotes + " / -" + ac.ForumTotalDownvotes);
            }
        }

        public static void GetForumKarmaVotes()
        {
            ZkDataContext db = new ZkDataContext();
            var posts = db.ForumPosts.Where(x => x.Upvotes > 0 || x.Downvotes > 0).OrderByDescending(x => x.Upvotes - x.Downvotes).ToList();

            foreach (var p in posts)
            {
                System.Console.WriteLine(p.ForumThreadID + "\t+" + p.ForumPostID + "\t+" + p.Upvotes + " / -" + p.Downvotes);
            }
        }

        public static void GetForumVotesByVoter(int? accountID, int? threadID)
        {
            ZkDataContext db = new ZkDataContext();
            var votes = db.AccountForumVotes.Where(x => (accountID == null || x.AccountID == accountID) && (threadID == null || x.ForumPost.ForumThreadID == threadID)).ToList();

            foreach (var v in votes)
            {
                System.Console.WriteLine(v.Account.Name + "\t+" + v.ForumPostID + "\t" + v.Vote);
            }
        }

        public static void DeleteUserVote(int accountID, int forumPostID, ZkDataContext db)
        {
            AccountForumVote existingVote = db.AccountForumVotes.SingleOrDefault(x => x.ForumPostID == forumPostID && x.AccountID == accountID);
            if (existingVote == null) return;

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
        }

        public static void DeleteUserVotes(int accountID, int? threadID, bool? onlyNegative = false)
        {
            ZkDataContext db = new ZkDataContext();
            var votes = db.AccountForumVotes.Where(x => x.AccountID == accountID && (threadID == null || x.ForumPost.ForumThreadID == threadID)
                && (onlyNegative == false || x.Vote < 0)).ToList();

            foreach (var v in votes)
            {
                DeleteUserVote(accountID, v.ForumPostID, db);
                System.Console.WriteLine(v.Account.Name + "\t" + v.ForumPostID + "\t" + v.Vote);
            }
            db.SubmitChanges();
        }

        public static void RecountForumVotes()
        {
            ZkDataContext db = new ZkDataContext();
            var accounts = db.Accounts.Where(x => x.ForumTotalUpvotes > 0 || x.ForumTotalDownvotes > 0).ToList();
            foreach (var acc in accounts)
            {
                acc.ForumTotalUpvotes = 0;
                acc.ForumTotalDownvotes = 0;
            }

            var votes = db.AccountForumVotes.ToList();
            foreach (var vote in votes)
            {
                Account poster = vote.ForumPost.Account;
                int delta = vote.Vote;
                if (delta > 0) poster.ForumTotalUpvotes = poster.ForumTotalUpvotes + delta;
                else if (delta < 0) poster.ForumTotalDownvotes = poster.ForumTotalDownvotes - delta;
            }
            db.SubmitChanges();
        }

        public static void GetForumVotesByUser(int voterID, int voteeID)
        {
            int up = 0, down = 0;
            ZkDataContext db = new ZkDataContext();
            Account voter = db.Accounts.FirstOrDefault(x => x.AccountID == voterID), votee = db.Accounts.FirstOrDefault(x => x.AccountID == voteeID);
            var votes = db.AccountForumVotes.Where(x => x.AccountID == voterID && x.ForumPost.AuthorAccountID == voteeID).ToList();
            System.Console.WriteLine(voter.Name + ", " + votee.Name);
            foreach (var vote in votes)
            {
                int delta = vote.Vote;
                if (delta > 0) up++;
                else if (delta < 0) down++;
                System.Console.WriteLine(vote.ForumPost.ForumThreadID + ", " + vote.ForumPostID);

            }
            System.Console.WriteLine(string.Format("+{0} / -{1}", up, down));
        }

        public static void GetForumVotesByUserVoterAgnostic(int voteeID)
        {
            int up = 0, down = 0;
            ZkDataContext db = new ZkDataContext();
            Account votee = db.Accounts.FirstOrDefault(x => x.AccountID == voteeID);
            var votes = db.AccountForumVotes.Where(x => x.ForumPost.AuthorAccountID == voteeID && x.Vote != 0).OrderBy(x => x.Account.Name).ToList();
            System.Console.WriteLine(votee.Name);
            foreach (var vote in votes)
            {
                int delta = vote.Vote;
                if (delta > 0) up++;
                else if (delta < 0) down++;
                System.Console.WriteLine(vote.ForumPost.ForumThreadID + ", " + vote.ForumPostID + ", " + vote.Account.Name);

            }
            System.Console.WriteLine(string.Format("+{0} / -{1}", up, down));
        }

        public static void GetForumVotesByPost(int voteeID)
        {
            int up = 0, down = 0;
            ZkDataContext db = new ZkDataContext();
            Account votee = db.Accounts.FirstOrDefault(x => x.AccountID == voteeID);
            var posts = db.ForumPosts.Where(x => x.AuthorAccountID == voteeID && (x.Upvotes > 0 || x.Downvotes > 0)).ToList();
            System.Console.WriteLine(votee.Name);
            foreach (var post in posts)
            {
                up = up + post.Upvotes;
                down = down + post.Downvotes;
                System.Console.WriteLine(post.ForumThreadID + ", " + post.ForumPostID + String.Format(" ({0}/{1})", post.Upvotes, post.Downvotes));
                //System.Console.WriteLine(vote.ForumPost.Text);
            }
            System.Console.WriteLine(string.Format("+{0} / -{1}", up, down));
        }
    }
}
