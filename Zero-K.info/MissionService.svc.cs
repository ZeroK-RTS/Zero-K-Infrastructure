using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.Linq;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ZkData.UnitSyncLib;
using ZkData;

namespace ZeroKWeb
{
	public class MissionService: IMissionService
	{
		const string MissionFileUrl = "http://zero-k.info/Missions/File/{0}";

		public void DeleteMission(int missionID, string author, string password)
		{
			var db = new ZkDataContext();
			var prev = db.Missions.Where(x => x.MissionID == missionID).SingleOrDefault();
			if (prev != null)
			{
				var acc = AuthServiceClient.VerifyAccountPlain(author, password);
				if (acc == null) throw new ApplicationException("Invalid login name or password");
				if (acc.AccountID != prev.AccountID && !acc.IsZeroKAdmin && !acc.IsLobbyAdministrator) throw new ApplicationException("You cannot delete a mission from an other user");
				prev.IsDeleted = true;
				db.SubmitChanges();
			}
			else throw new ApplicationException("No such mission found");
		}

		public void UndeleteMission(int missionID, string author, string password)
		{
			var db = new ZkDataContext();
			var prev = db.Missions.Where(x => x.MissionID == missionID).SingleOrDefault();
			if (prev != null)
			{
				var acc = AuthServiceClient.VerifyAccountPlain(author, password);
				if (acc == null) throw new ApplicationException("Invalid login name or password");
				if (acc.AccountID != prev.AccountID && !acc.IsZeroKAdmin && !acc.IsLobbyAdministrator) throw new ApplicationException("You cannot undelete a mission from an other user");
				prev.IsDeleted = false;
				db.SubmitChanges();
			}
			else throw new ApplicationException("No such mission found");
		}

		public Mission GetMission(string missionName)
		{
			var db = new ZkDataContext();
			var prev = db.Missions.Where(x => x.Name == missionName).Include(x=>x.Mutator).SingleOrDefault();
			db.SubmitChanges();
			return prev;
		}

		public Mission GetMissionByID(int missionID)
		{
			var db = new ZkDataContext();
			var prev = db.Missions.Where(x => x.MissionID == missionID).Include(x=>x.Mutator).SingleOrDefault();
			db.SubmitChanges();
			return prev;
		}

	    public IEnumerable<Mission> ListMissionInfos()
	    {
	        var db = new ZkDataContext();
	        var list = db.Missions.ToList();
	        foreach (var m in list) {
	            m.Mutator = new byte[] { };
	            m.Script = null;
	            m.Image = new byte[] { };
	        }
	        return list;
	    }


	    public void SendMission(Mission mission, List<MissionSlot> slots, string author, string password, Mod modInfo)
		{
            if (mission == null) throw new ApplicationException("Mission is null");

			Account acc = null;
			var db = new ZkDataContext();
			if (Debugger.IsAttached) acc = db.Accounts.SingleOrDefault(x => x.Name == "Testor303");
			else acc = AuthServiceClient.VerifyAccountPlain(author, password);

			if (acc == null) throw new ApplicationException("Cannot verify user account");


            Mission prev = db.Missions.SingleOrDefault(x => x.MissionID == mission.MissionID || (x.Name == mission.Name && x.AccountID == acc.AccountID)); // previous mission by id or name + account
			if (prev == null && db.Missions.Any(x =>x.Name == mission.Name)) throw new ApplicationException("Mission name must be unique");
			var map = db.Resources.SingleOrDefault(x => x.InternalName == mission.Map && x.TypeID == ZkData.ResourceType.Map);
			if (map == null) throw new ApplicationException("Map name is unknown");
			var mod = db.Resources.SingleOrDefault(x => x.InternalName == mission.Mod && x.TypeID == ZkData.ResourceType.Mod);
			if (mod == null) throw new ApplicationException("Mod name is unknown");
			//if (db.Resources.Any(x => x.InternalName == mission.Name && x.MissionID != null)) throw new ApplicationException("Name already taken by other mod/map");

            modInfo.MissionMap = mission.Map;

			if (prev != null)
			{
				if (prev.AccountID != acc.AccountID && !acc.IsLobbyAdministrator && !acc.IsZeroKAdmin) throw new ApplicationException("Invalid author or password");
				prev.Description = mission.Description;
                prev.DescriptionStory = mission.DescriptionStory;
				prev.Mod = mission.Mod;
				prev.Map = mission.Map;
				prev.Name = mission.Name;
				prev.ScoringMethod = mission.ScoringMethod;
				prev.ModRapidTag = mission.ModRapidTag;
				prev.ModOptions = mission.ModOptions;
				prev.Image = mission.Image;
				prev.MissionEditorVersion = mission.MissionEditorVersion;
				prev.SpringVersion = mission.SpringVersion;
				prev.Revision++;
				prev.Mutator = mission.Mutator;
                prev.ForumThread.Title = mission.Name;
				mission = prev;
			}
			else
			{
				mission.CreatedTime = DateTime.UtcNow;
                mission.ForumThread = new ForumThread() { Title = mission.Name, ForumCategory = db.ForumCategories.FirstOrDefault(x=>x.IsMissions), CreatedAccountID = acc.AccountID, LastPostAccountID= acc.AccountID };
                mission.ForumThread.UpdateLastRead(acc.AccountID, true);
				db.Missions.InsertOnSubmit(mission);
			}
			mission.AccountID = acc.AccountID;
			mission.Script = Regex.Replace(mission.Script, "GameType=([^;]+);", (m) => { return string.Format("GameType={0};", mission.NameWithVersion); });
			mission.MinHumans = slots.Count(x => x.IsHuman && x.IsRequired);
			mission.MaxHumans = slots.Count(x => x.IsHuman);
			mission.ModifiedTime = DateTime.UtcNow;
			mission.IsDeleted = true;
			mission.IsCoop = slots.Where(x => x.IsHuman).GroupBy(x => x.AllyID).Count() == 1;

			db.SubmitChanges();

            var updater = new MissionUpdater();
            updater.UpdateMission(db, mission, modInfo);

			mission.IsDeleted = false;
			db.SubmitChanges();
		}
	}
}