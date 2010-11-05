using System;
using System.ComponentModel;
using System.Linq;
using System.Transactions;
using System.Web.Services;
using ZkData;

namespace ZeroKWeb
{
	/// <summary>
	/// Summary description for ContentService
	/// </summary>
	[WebService(Namespace = "http://tempuri.org/")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[ToolboxItem(false)]
	// To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
		// [System.Web.Script.Services.ScriptService]
	public class ContentService: WebService
	{
		[WebMethod]
		public void NotifyMissionRun(string login, string missionName)
		{
			using (var db = new ZkDataContext())
			{
				using (var scope = new TransactionScope())
				{
					db.Missions.Single(x => x.Name == missionName).MissionRunCount++;
					db.Accounts.Single(x => x.Name == login).MissionRunCount++;
					db.SubmitChanges();
					scope.Complete();
				}
			}

		}
		
		[WebMethod]
		public void SubmitMissionScore(string login, string passwordHash, string missionName, int score, int gameSeconds)
		{
			using (var db = new ZkDataContext())
			{
				var auth = new AuthServiceClient();
				var acc = auth.VerifyAccountHashed(login, passwordHash);
				if (acc == null) throw new ApplicationException("Invalid login or password");

				var mission = db.Missions.Single(x => x.Name == missionName);

				var scoreEntry = mission.MissionScores.FirstOrDefault(x => x.AccountID == acc.AccountID);
				if (scoreEntry == null)
				{
					scoreEntry = new MissionScore() { MissionID = mission.MissionID, AccountID = acc.AccountID, Score = int.MinValue };
					mission.MissionScores.Add(scoreEntry);
				}

				if (score > scoreEntry.Score)
				{
					scoreEntry.Score = score;
					scoreEntry.Time = DateTime.UtcNow;
					scoreEntry.MissionRevision = mission.Revision;
					scoreEntry.GameSeconds = gameSeconds;
					db.SubmitChanges();
				}
			}
		}

		[WebMethod]
		public void SubmitStackTrace(ProgramType programType, string playerName, string exception, string extraData)
		{
			using (var db = new ZkDataContext())
			{
				var exceptionLog = new ExceptionLog
				                   {
				                   	ProgramID = programType,
				                   	Time = DateTime.UtcNow,
				                   	PlayerName = playerName,
				                   	ExtraData = extraData,
				                   	Exception = exception,
				                   	RemoteIP = GetUserIP()
				                   };
				db.ExceptionLogs.InsertOnSubmit(exceptionLog);
				db.SubmitChanges();
			}
		}

		string GetUserIP()
		{
			var ip = Context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
			if (string.IsNullOrEmpty(ip) || ip.Equals("unknown", StringComparison.OrdinalIgnoreCase)) ip = Context.Request.ServerVariables["REMOTE_ADDR"];
			return ip;
		}
	}
}