using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Transactions;
using ZeroKWeb.missions;
using ZkData;

namespace ZeroKWeb
{
	// NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "MissionService" in code, svc and config file together.
	public class MissionService: IMissionService
	{
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

		public Mission GetMission(string missionName)
		{
			var db = new ZkDataContext();
			var opt = new DataLoadOptions();
			opt.LoadWith<Mission>(x => x.Mutator);
			opt.LoadWith<Mission>(x => x.Script);
			opt.LoadWith<Mission>(x => x.Account);
			db.LoadOptions = opt;
			var prev = db.Missions.Where(x => x.Name == missionName).SingleOrDefault();
			prev.DownloadCount++;
			db.SubmitChanges();
			return prev;
		}

		public Mission GetMissionByID(int missionID)
		{
			var db = new ZkDataContext();
			var opt = new DataLoadOptions();
			opt.LoadWith<Mission>(x => x.Mutator);
			opt.LoadWith<Mission>(x => x.Script);
			opt.LoadWith<Mission>(x => x.Account);
			db.LoadOptions = opt;
			var prev = db.Missions.Where(x => x.MissionID == missionID).SingleOrDefault();
			prev.DownloadCount++;
			db.SubmitChanges();
			return prev;
		}

		public IEnumerable<Mission> ListMissionInfos()
		{
			var db = new ZkDataContext();
			var opt = new DataLoadOptions();
			opt.LoadWith<Mission>(x => x.Account);
			db.LoadOptions = opt;
			var list = db.Missions.ToList();
			foreach (var m in list)
			{
				m.Mutator = new Binary(new byte[] { });
				m.Script = null;
			}
			return list;
		}

		public void SendMission(Mission mission, string author, string password)
		{
			var acc = new AuthServiceClient().VerifyAccount(author, password);
			if (acc == null) throw new ApplicationException("Cannot verify user account");
			var db = new ZkDataContext();
			if (!mission.Name.StartsWith("Mission:")) throw new ApplicationException("Mission name must start with Mission:, please update your editor");
			var prev = db.Missions.Where(x => x.MissionID == mission.MissionID).SingleOrDefault();
			if (prev == null) prev = db.Missions.Where(x => x.Name == mission.Name).SingleOrDefault();

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
				mission.ModifiedTime = DateTime.UtcNow;
				db.Missions.InsertOnSubmit(mission);
				db.SubmitChanges();
			}
		}
	}
}