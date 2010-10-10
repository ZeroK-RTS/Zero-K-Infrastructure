#region using

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;

#endregion

namespace ModelBase
{
	public class ForumController
	{
		#region Constants

		private const string Bbbitcode = "fA==";
		private const int ForumId = 9;
		private const int PosterId = 8603;
		private const string PosterIp = "91.121.98.29";
		private const int TopicIdModels = 19049;
		private const int TopicIdNews = 19052;

		#endregion

		#region Public methods

		public void MakeModelPosts()
		{
			foreach (Model m in Global.Db.Models) {
				string bbuid = GenBBUid();
				string text = string.Format("[url={0}:{1}][img:{1}]{2}[/img:{1}][size=150:{1}]  [b:{1}]{3}[/b:{1}][/size:{1}][/url:{1}]\n {4}",
				                            EscapeBB(Global.BaseUrl + "ModelDetail.aspx?ModelID=" + m.ModelID),
				                            bbuid,
				                            EscapeBB(Global.BaseUrl + "modelicons/" + m.ModelID + ".png"),
				                            m.Name + " by " + m.User.Login,
				                            m.Description);

				m.ForumPostID = PostOrEdit(text, bbuid, m.ForumPostID, TopicIdModels, m.Name + " by " + m.User.Login);
				Global.Db.SubmitChanges();
			}
		}

		public void MakeNewsPosts()
		{
			DateTime lastDay = DateTime.MinValue;
			DateTime today = RoundToDay(DateTime.UtcNow);
			string bbuid = GenBBUid();
			Dictionary<int, StringBuilder> dayModels = new Dictionary<int, StringBuilder>();

			foreach (Event e in Global.Db.Events.Where(x => x.Type == EventType.ModelAdded || x.Type == EventType.ModelUpdated).OrderBy(x => x.Time)) {
				DateTime day = RoundToDay(e.Time);

				if (day != lastDay) {
					if (lastDay == DateTime.MinValue) lastDay = day;
					else {
						StringBuilder text = new StringBuilder();
						foreach (StringBuilder t in dayModels.Values) text.Append(t);
						if (lastDay != today) PostNews(text.ToString(), bbuid, lastDay);

						bbuid = GenBBUid();
						lastDay = day;
						dayModels = new Dictionary<int, StringBuilder>();
					}
				}

				StringBuilder sb;
				if (!dayModels.TryGetValue(e.ModelID.Value, out sb)) {
					sb = new StringBuilder();
					dayModels[e.ModelID.Value] = sb;

					sb.AppendFormat("[url={0}:{1}][img:{1}]{2}[/img:{1}][size=150:{1}]  [b:{1}]{3}[/b:{1}][/size:{1}][/url:{1}]\n[{7}] {4} {5}\n{6}",
					                EscapeBB(Global.BaseUrl + "ModelDetail.aspx?ModelID=" + e.ModelID),
					                bbuid,
					                EscapeBB(Global.BaseUrl + "modelicons/" + e.ModelID + ".png"),
					                e.Model.Name + " by " + e.Model.User.Login,
					                e.Summary,
					                e.Text,
					                e.SvnLog, 
									e.Time);
				} else sb.AppendFormat("[{3}] {0} {1}\n{2}", e.Summary, e.Text, e.SvnLog, e.Time);
			}
			if (dayModels.Count > 0) {
				StringBuilder text = new StringBuilder();
				foreach (StringBuilder t in dayModels.Values) text.Append(t);
				if (lastDay != today) PostNews(text.ToString(), bbuid, lastDay);
			}
		}

		#endregion

		#region Other methods

		private static string EscapeBB(string data)
		{
			string ret = data.Replace(":", "&#58;");
			ret = ret.Replace(".", "&#46;");
			return ret;
		}

		private static string GenBBUid()
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

		private void PostNews(string text, string bbuid, DateTime lastDay)
		{
			ForumNewsPost post = Global.Db.ForumNewsPosts.Where(x => x.Day == lastDay).SingleOrDefault();
			if (post == null) {
				post = new ForumNewsPost();
				post.Day = lastDay;
				Global.Db.ForumNewsPosts.InsertOnSubmit(post);
			}
			post.ForumPostID = PostOrEdit(text, bbuid, post.ForumPostID, TopicIdNews, "Daily news " + lastDay);
			Global.Db.SubmitChanges();
		}

		private static int PostOrEdit(string text, string bbuid, int? postID, uint topicID, string postSubject)
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