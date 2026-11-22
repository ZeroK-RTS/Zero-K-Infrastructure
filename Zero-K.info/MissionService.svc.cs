using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ZkData.UnitSyncLib;
using ZkData;

namespace ZeroKWeb
{
	/// <summary>
	/// The WCF endpoint, which MissionService.svc points at. **Deprecated**: server-side WCF
	/// has no successor on .NET 9, and /MissionService carries the same six operations in JSON.
	///
	/// It still serves, because mission editors installed before that endpoint existed use it
	/// and only pick up the new one when ClickOnce updates them - which happens on launch, not
	/// on deploy. Removing it before then breaks publishing for anyone who has not opened the
	/// editor since.
	///
	/// **So it says who is still calling it.** Every operation logs, and SendMission logs the
	/// caller's editor version, which is the number that decides when this can go. That plus
	/// the MissionEditorVersion column - written on every publish, through either endpoint -
	/// answers "has everyone updated?" from the site's own data.
	///
	/// ContentService.svc reports itself too now, but differently and for a reason: publishing
	/// a mission is rare, so this one can afford a line per call, and that line carries the
	/// editor version the retirement turns on. That endpoint's traffic is unknown and every
	/// trace is a database insert, so it counts rather than logs. See LegacyCallReporter.
	///
	/// The logic is in MissionServiceLogic below and is shared with the JSON endpoint, so this
	/// class can be deleted along with the .svc file in one commit when the time comes.
	/// </summary>
	public class MissionService: IMissionService
	{
		private readonly MissionServiceLogic logic = new MissionServiceLogic();

		public void DeleteMission(int missionID, string author, string password)
		{
			LogLegacyCall("DeleteMission", author, null);
			logic.DeleteMission(missionID, author, password);
		}

		public void UndeleteMission(int missionID, string author, string password)
		{
			LogLegacyCall("UndeleteMission", author, null);
			logic.UndeleteMission(missionID, author, password);
		}

		public Mission GetMission(string missionName)
		{
			LogLegacyCall("GetMission", null, null);
			return logic.GetMission(missionName);
		}

		public Mission GetMissionByID(int missionID)
		{
			LogLegacyCall("GetMissionByID", null, null);
			return logic.GetMissionByID(missionID);
		}

		public IEnumerable<Mission> ListMissionInfos()
		{
			LogLegacyCall("ListMissionInfos", null, null);
			return logic.ListMissionInfos();
		}

		public void SendMission(Mission mission, List<MissionSlot> slots, string author, string password, Mod modInfo)
		{
			// The one call that knows which editor is asking.
			LogLegacyCall("SendMission", author, mission?.MissionEditorVersion);
			logic.SendMission(mission, slots, author, password, modInfo);
		}

		/// <summary>
		/// Goes to LogEntries, which Admin/TraceLogs shows and which keeps 14 days - long enough
		/// to answer whether anyone is still on the WCF path, short enough not to accumulate.
		/// </summary>
		static void LogLegacyCall(string operation, string author, string editorVersion)
		{
			Trace.TraceInformation(
				"MissionService.svc (deprecated WCF endpoint): {0} by {1}, mission editor {2}",
				operation, author ?? "anonymous", editorVersion ?? "version not reported");
		}
	}

	/// <summary>The operations themselves, shared by the WCF endpoint above and /MissionService.</summary>
	public class MissionServiceLogic: IMissionService
	{
		public void DeleteMission(int missionID, string author, string password)
		{
			var db = new ZkDataContext();
			var prev = db.Missions.Where(x => x.MissionID == missionID).SingleOrDefault();
			if (prev != null)
			{
				var acc = AuthServiceClient.VerifyAccountPlain(author, password);
				if (acc == null)
				{
					Trace.TraceWarning("Invalid login attempt for {0}", author);
					System.Threading.Thread.Sleep(new Random().Next(2000));
					throw new ApplicationException("Cannot verify user account");
				}
				if (acc.AccountID != prev.AccountID && acc.AdminLevel < AdminLevel.Moderator) throw new ApplicationException("You cannot delete a mission from an other user");
				prev.IsDeleted = true;
				db.SaveChanges();
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
				if (acc == null)
				{
					Trace.TraceWarning("Invalid login attempt for {0}", author);
					System.Threading.Thread.Sleep(new Random().Next(2000));
					throw new ApplicationException("Cannot verify user account");
				}

				if (acc.AccountID != prev.AccountID && acc.AdminLevel < AdminLevel.Moderator) throw new ApplicationException("You cannot undelete a mission from an other user");
				prev.IsDeleted = false;
				db.SaveChanges();
			}
			else throw new ApplicationException("No such mission found");
		}

		public Mission GetMission(string missionName)
		{
			var db = new ZkDataContext();
			db.Configuration.ProxyCreationEnabled = false;
			var prev = db.Missions.Where(x => x.Name == missionName).Include(x=>x.Mutator).SingleOrDefault();
			return prev;
		}

		public Mission GetMissionByID(int missionID)
		{
			var db = new ZkDataContext();
			db.Configuration.ProxyCreationEnabled = false;
			var prev = db.Missions.Where(x => x.MissionID == missionID).Include(x=>x.Mutator).SingleOrDefault();
			return prev;
		}

		public IEnumerable<Mission> ListMissionInfos()
		{
			var db = new ZkDataContext();
			db.Configuration.ProxyCreationEnabled = false;
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

			if (acc == null)
			{
				Trace.TraceWarning("Invalid login attempt for {0}", author);
				System.Threading.Thread.Sleep(new Random().Next(2000));
				throw new ApplicationException("Cannot verify user account");
			}


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
				if (prev.AccountID != acc.AccountID && acc.AdminLevel < AdminLevel.Moderator) throw new ApplicationException("Invalid author or password");
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
				prev.Script = mission.Script;
				mission = prev;
			}
			else
			{
				mission.CreatedTime = DateTime.UtcNow;
				mission.ForumThread = new ForumThread() { Title = mission.Name, ForumCategory = db.ForumCategories.FirstOrDefault(x=>x.ForumMode==ForumMode.Missions), CreatedAccountID = acc.AccountID, LastPostAccountID= acc.AccountID };
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

			db.SaveChanges();

			var updater = new MissionUpdater();
			updater.UpdateMission(db, mission, modInfo);

			mission.IsDeleted = false;
			db.SaveChanges();
		}
	}
}