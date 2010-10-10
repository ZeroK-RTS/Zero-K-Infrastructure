#region using

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Web.Services;

#endregion

namespace MissionEditorServer
{
	/// <summary>
	/// Summary description for EditorService
	/// </summary>
	[WebService(Namespace = "http://SpringMissionEditor/")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[ToolboxItem(false)]
	// To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
		// [System.Web.Script.Services.ScriptService]
	public class EditorService : WebService
	{
		#region Public methods

		[WebMethod]
		public void AddComment(string nick, int missionID, string text)
		{
			dbDataContext db = GetContext();
			db.Comments.InsertOnSubmit(new Comment {MissionID = missionID, Time = DateTime.UtcNow, Text = text, Nick = nick});
			db.SubmitChanges();
			Mission mis = db.Missions.Where(x => x.MissionID == missionID).Single();
			mis.LastCommentTime = DateTime.UtcNow;
			mis.CommentCount = mis.Comments.Count;
			db.SubmitChanges();
		}

		[WebMethod]
		public void DeleteMission(int missionID, string author, string password)
		{
			dbDataContext db = GetContext();
			Mission prev = db.Missions.Where(x => x.MissionID == missionID).SingleOrDefault();
			if (prev != null) {
				if (prev.Author != author || prev.Password != password) throw new ApplicationException("Invalid author or password");
				db.Missions.DeleteOnSubmit(prev);
				db.SubmitChanges();
			} else throw new ApplicationException("No such mission found");
		}

		public static dbDataContext GetContext()
		{
			dbDataContext db = new dbDataContext(ConfigurationManager.ConnectionStrings["EditorServer"].ConnectionString);
			return db;
		}

		[WebMethod]
		public MissionData GetMission(string missionName)
		{
			dbDataContext db = GetContext();
			Mission prev = db.Missions.Where(x => x.Name == missionName).SingleOrDefault();
			prev.DownloadCount++;
			db.SubmitChanges();
			return new MissionData(prev);
		}

		[WebMethod]
		public MissionData GetMissionByID(int missionID)
		{
			dbDataContext db = GetContext();
			Mission prev = db.Missions.Where(x => x.MissionID == missionID).SingleOrDefault();
			prev.DownloadCount++;
			db.SubmitChanges();
			return new MissionData(prev);
		}

		[WebMethod]
		public List<ScoreEntry> GetScores(int missionID)
		{
			dbDataContext db = GetContext();
			return db.Scores.Where(x => x.MissionID == missionID).OrderByDescending(x => x.Score1).Select(x => new ScoreEntry {PlayerName = x.PlayerName, Score = x.Score1, TimeSeconds = x.TimeSeconds}).ToList();
		}


		[WebMethod]
		public List<CommentInfo> ListComments(int missionID)
		{
			dbDataContext db = GetContext();
			return db.Comments.Where(x => x.MissionID == missionID).Select(x => new CommentInfo {Nick = x.Nick, Text = x.Text, Time = x.Time}).ToList();
		}

		[WebMethod]
		public List<MissionInfo> ListMissionInfos()
		{
			dbDataContext db = GetContext();
			return db.Missions.Select(x => new MissionInfo(x)).ToList();
		}


		[WebMethod]
		public void Rate(int missionID, double rating)
		{
			if (rating > 10 || rating < 0) throw new ArgumentException("Rating outside range");
			dbDataContext db = GetContext();
			Mission prev = db.Missions.Where(x => x.MissionID == missionID).SingleOrDefault();
			if (prev == null) throw new ApplicationException("No such mission found");

			string ip = GetUserIP();

			Rating vote = db.Ratings.Where(x => x.MissionID == missionID && x.IP == ip).SingleOrDefault();

			if (vote == null) {
				vote = new Rating();
				vote.MissionID = missionID;
				vote.IP = ip;
				vote.Rating1 = rating;
				db.Ratings.InsertOnSubmit(vote);
			} else vote.Rating1 = rating;

			db.SubmitChanges();

			prev.Rating = prev.Ratings.Average(x => x.Rating1);
			db.SubmitChanges();
		}

		public static string SecondsToTime(int seconds)
		{
			return string.Format("{0}:{1:00}", seconds/60, seconds%60);
		}


		[WebMethod]
		public void SendMission(MissionData mission, string author, string password)
		{
			dbDataContext db = GetContext();
			if (!mission.MissionInfo.Name.StartsWith("Mission:")) throw new ApplicationException("Mission name must start with Mission:, please update your editor");
			Mission prev = db.Missions.Where(x => x.MissionID == mission.MissionInfo.MissionID).SingleOrDefault();


			bool byName = false;
			if (prev == null) {
				prev = db.Missions.Where(x => x.Name == mission.MissionInfo.Name).SingleOrDefault();
				byName = true;
			}
			
			if (prev != null) {
				if (prev.Author != author || prev.Password != password) throw new ApplicationException("Invalid author or password");
				if (!byName && prev.Name == mission.MissionInfo.Name) throw new ApplicationException("When replacing existing mission, change its name");

				prev.Mod = mission.MissionInfo.Mod;
				prev.Map = mission.MissionInfo.Map;
				prev.Image = mission.MissionInfo.Image;
				prev.Name = mission.MissionInfo.Name;
				prev.Mutator = mission.Mutator;
				prev.Description = mission.MissionInfo.Description;
				prev.ModifiedTime = DateTime.UtcNow;
				prev.ScoringMethod = mission.MissionInfo.ScoringMethod;
				prev.SpringVersion = mission.MissionInfo.SpringVersion;
				prev.MissionEditorVersion = mission.MissionInfo.MissionEditorVersion;
				if (prev.CreatedTime == null) prev.CreatedTime = prev.ModifiedTime;
			} else {
				Mission m = new Mission();
				m.Mod = mission.MissionInfo.Mod;
				m.Map = mission.MissionInfo.Map;
				m.Mutator = mission.Mutator;
				m.Image = mission.MissionInfo.Image;
				m.Name = mission.MissionInfo.Name;
				m.Description = mission.MissionInfo.Description;
				m.ScoringMethod = mission.MissionInfo.ScoringMethod;
				m.Author = author;
				m.Password = password;
				m.ModifiedTime = DateTime.UtcNow;
				m.CreatedTime = m.ModifiedTime;
				m.SpringVersion = mission.MissionInfo.SpringVersion;
				m.MissionEditorVersion = mission.MissionInfo.MissionEditorVersion;
				db.Missions.InsertOnSubmit(m);
			}
			db.SubmitChanges();
		}

		[WebMethod]
		public void SubmitScore(string missionName, string playerName, int score, int timeSeconds)
		{
			dbDataContext db = GetContext();
			Mission mission = db.Missions.Where(x => x.Name == missionName).Single();
			Score prev = db.Scores.Where(x => x.MissionID == mission.MissionID && x.PlayerName == playerName).SingleOrDefault();
			Score ns = prev;
			if (prev != null) {
				if (prev.Score1 > score) return; // do not save, previous score bigger
				ns = prev;
			} else ns = new Score();

			ns.MissionID = mission.MissionID;
			ns.PlayerName = playerName;
			ns.Score1 = score;
			ns.TimeSeconds = timeSeconds;
			ns.IP = GetUserIP();

			if (prev == null) db.Scores.InsertOnSubmit(ns);
			db.SubmitChanges();

			Score best = db.Scores.Where(x => x.MissionID == mission.MissionID && x.Score1 >= db.Scores.Where(y=>y.MissionID == mission.MissionID).Max(y => y.Score1)).First();
			mission.TopScoreLine = string.Format("{0} ({1} in {2})", best.PlayerName, best.Score1, SecondsToTime(best.TimeSeconds));
			db.SubmitChanges();
		}

		#endregion

		#region Other methods

		private string GetUserIP()
		{
			string ip = Context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
			if (string.IsNullOrEmpty(ip) || ip.Equals("unknown", StringComparison.OrdinalIgnoreCase)) ip = Context.Request.ServerVariables["REMOTE_ADDR"];
			return ip;
		}

		#endregion

		#region Nested type: CommentInfo

		public class CommentInfo
		{
			#region Properties

			public string Nick;
			public string Text;
			public DateTime Time;

			#endregion
		}

		#endregion
	}
}