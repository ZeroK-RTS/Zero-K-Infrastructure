using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq;
using System.Linq;
using System.Transactions;
using System.Web.Services;
using ZkData;

namespace asp.net.missions
{
	/// <summary>
	/// Summary description for MissionService
	/// </summary>
	[WebService(Namespace = "http://SpringMissionEditor/")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[ToolboxItem(false)]
	// To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
		// [System.Web.Script.Services.ScriptService]
	public class MissionService: WebService
	{
		[WebMethod]
		public void DeleteMission(int missionID, string author, string password)
		{
			var db = new ZkDataContext();
			var auth = new AuthServiceClient();
			var prev = db.Missions.Where(x => x.MissionID == missionID).SingleOrDefault();
			if (prev != null)
			{
				var acc = auth.VerifyAccount(author, password);
				if (acc == null || acc.AccountID != prev.AccountID) throw new ApplicationException("Invalid author or password");
				db.Missions.DeleteOnSubmit(prev);
				db.SubmitChanges();
			}
			else throw new ApplicationException("No such mission found");
		}

		/// <summary>
		/// Downloads full mission data
		/// </summary>
		/// <param name="missionName"></param>
		/// <returns></returns>
		[WebMethod]
		public Mission GetMission(string missionName)
		{
			var db = new ZkDataContext();
			var opt = new DataLoadOptions();
			opt.LoadWith<Mission>(x => x.Mutator);
			opt.LoadWith<Mission>(x => x.Script);
			var prev = db.Missions.Where(x => x.Name == missionName).SingleOrDefault();
			prev.DownloadCount++;
			db.SubmitChanges();
			return prev;
		}


		[WebMethod]
		public Mission GetMissionByID(int missionID)
		{
			var db = new ZkDataContext();
			var opt = new DataLoadOptions();
			opt.LoadWith<Mission>(x => x.Mutator);
			opt.LoadWith<Mission>(x => x.Script);
			var prev = db.Missions.Where(x => x.MissionID == missionID).SingleOrDefault();
			prev.DownloadCount++;
			db.SubmitChanges();
			return prev;
		}

		[WebMethod]
		public List<Mission> ListMissionInfos()
		{
			var db = new ZkDataContext();
			return db.Missions.ToList();
		}

		[WebMethod]
		public void SendMission(Mission mission, string author, string password)
		{
			var acc = new AuthServiceClient().VerifyAccount(author, password);
			if (acc == null) throw new ApplicationException("Cannot verify user account");
			var db = new ZkDataContext();
			if (!mission.Name.StartsWith("Mission:")) throw new ApplicationException("Mission name must start with Mission:, please update your editor");
			var prev = db.Missions.Where(x => x.MissionID == mission.MissionID).SingleOrDefault();

			var byName = false;
			if (prev == null)
			{
				prev = db.Missions.Where(x => x.Name == mission.Name).SingleOrDefault();
				byName = true;
			}

			if (prev != null)
			{
				if (prev.AccountID != acc.AccountID) throw new ApplicationException("Invalid author or password");
				using (var scope = new TransactionScope())
				{
					db.MissionSlots.DeleteAllOnSubmit(prev.MissionSlots);
					db.SubmitChanges();
					db.Missions.Attach(mission, prev);
					db.MissionSlots.AttachAll(prev.MissionSlots);

					mission.Revision++;
					mission.ModifiedTime = DateTime.UtcNow;
					db.SubmitChanges();
					scope.Complete();
				}
			}
			else
			{
				mission.CreatedTime = DateTime.UtcNow;
				db.Missions.InsertOnSubmit(mission);
				db.SubmitChanges();
			}
		}

		/// <summary>
		/// Todo for reuse
		/// </summary>
		/// <returns></returns>
		string GetUserIP()
		{
			var ip = Context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
			if (string.IsNullOrEmpty(ip) || ip.Equals("unknown", StringComparison.OrdinalIgnoreCase)) ip = Context.Request.ServerVariables["REMOTE_ADDR"];
			return ip;
		}
	}
}