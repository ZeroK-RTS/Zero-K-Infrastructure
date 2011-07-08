using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using DbLinq.MySql;
using ModelBase;
using MySql.Data.MySqlClient;

namespace ZeroKWeb
{
	public class SpringForumController
	{

		#region Constants

		private const string Bbbitcode = "fA==";
		private const int ForumId = 42;
		private const int PosterId = 2037;
		private const string PosterIp = "91.121.98.29";
		//private const int TopicIdModels = 19049;
		//private const int TopicIdModelNews = 19052;
		public const int TopicIdNews = 26425;

		#endregion

		#region Public methods


		#endregion

		#region Other methods

		private static string EscapeBB(string data)
		{
			string ret = data.Replace(":", "&#58;");
			ret = ret.Replace(".", "&#46;");
			return ret;
		}

		public static string GenBBUid()
		{
			Random r = new Random();
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < 8; i++) {
				int v = r.Next(36);
				if (v >= 26) sb.Append((char) ('0' + v - 26));
				else sb.Append((char) ('a' + v));
			}
			return sb.ToString();
		}


		public static int PostOrEdit(string text, string bbuid, int? postID, uint topicID, string postSubject)
		{
			Spring db = new Spring(new MySqlConnection(ConfigurationManager.AppSettings["SpringConnectionString"]));
			PhPbB3Posts post;
			if (postID != null && postID.Value != 0 && (post = db.PhPbB3Posts.Where(x => x.PostID == postID).SingleOrDefault()) != null) {
				post.PostText = text;
				post.BbcOdeUID = bbuid;
				post.PostSubject = postSubject;
				db.SubmitChanges();
				return postID.Value;
			} else {
				uint time = ToUnix(DateTime.UtcNow);

				post = new PhPbB3Posts();
				post.ForumID = ForumId;
				post.PosterID = PosterId;
				post.PostSubject = postSubject;
				post.BbcOdeBitField = Bbbitcode;
				post.BbcOdeUID = bbuid;
				post.TopicID = topicID;
				post.PostTime = time;
				post.PostText = text;
				post.PosterIP = PosterIp;
				post.PostEditReason = "";
				post.PostChecksum = "";
				post.PostApproved = 1;
				post.EnableBbcOde = 1;
				post.EnableSmILies = 1;
				post.EnableMagicURL = 1;

				post.PostUserName = "";
				db.PhPbB3Posts.InsertOnSubmit(post);

				db.SubmitChanges();

				PhPbB3Topics topic = db.PhPbB3Topics.Where(x => x.TopicID == post.TopicID).Single();
				topic.TopicLastPostID = post.PostID;
				topic.TopicLastPostTime = time;
				topic.TopicRepliesReal++;
				topic.TopicReplies++;

				db.SubmitChanges();
				return (int) post.PostID;
			}
		}

		private static DateTime RoundToDay(DateTime d)
		{
			return new DateTime(d.Year, d.Month, d.Day);
		}

		private static uint ToUnix(DateTime t)
		{
			if (t == DateTime.MinValue) return 0;
			return (uint) (t.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
		}

		#endregion
	}
}
