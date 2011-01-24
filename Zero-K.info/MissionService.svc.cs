using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Linq;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MonoTorrent.Common;
using PlasmaShared;
using PlasmaShared.UnitSyncLib;
using ZkData;

namespace ZeroKWeb
{
	public class MissionService: IMissionService
	{
		const string MissionFileUrl = "http://zero-k.info/Missions.mvc/File/{0}";

		public void DeleteMission(int missionID, string author, string password)
		{
			var db = new ZkDataContext();
			var prev = db.Missions.Where(x => x.MissionID == missionID).SingleOrDefault();
			if (prev != null)
			{
				var acc = AuthServiceClient.VerifyAccountPlain(author, password);
				if (acc == null) throw new ApplicationException("Invalid login name or password");
				if (acc.AccountID != prev.AccountID && !acc.IsAdmin) throw new ApplicationException("You cannot delete a mission from an other user");
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
				if (acc.AccountID != prev.AccountID && !acc.IsAdmin) throw new ApplicationException("You cannot undelete a mission from an other user");
				prev.IsDeleted = false;
				db.SubmitChanges();
			}
			else throw new ApplicationException("No such mission found");
		}

		public Mission GetMission(string missionName)
		{
			var db = new ZkDataContext();
			var opt = new DataLoadOptions();
			opt.LoadWith<Mission>(x => x.Mutator);
			db.LoadOptions = opt;
			var prev = db.Missions.Where(x => x.Name == missionName).SingleOrDefault();
			db.SubmitChanges();
			return prev;
		}

		public Mission GetMissionByID(int missionID)
		{
			var db = new ZkDataContext();
			var opt = new DataLoadOptions();
			opt.LoadWith<Mission>(x => x.Mutator);
			db.LoadOptions = opt;
			var prev = db.Missions.Where(x => x.MissionID == missionID).SingleOrDefault();
			db.SubmitChanges();
			return prev;
		}

		public IEnumerable<Mission> ListMissionInfos()
		{
			var db = new ZkDataContext();
			var list = db.Missions.ToList();
			foreach (var m in list)
			{
				m.Mutator = new Binary(new byte[] { });
				m.Script = null;
				m.Image = new Binary(new byte[] { });
			}
			return list;
		}


		public void SendMission(Mission mission, List<MissionSlot> slots, string author, string password, Mod modInfo)
		{
			Account acc = null;
			var db = new ZkDataContext();
			if (Debugger.IsAttached) acc = db.Accounts.SingleOrDefault(x => x.Name == "Testor303");
			else acc = AuthServiceClient.VerifyAccountPlain(author, password);

			if (acc == null) throw new ApplicationException("Cannot verify user account");

			if (db.Missions.Any(x => x.Name == mission.Name)) throw new ApplicationException("Mission name must be unique");
			var map = db.Resources.SingleOrDefault(x => x.InternalName == mission.Map && x.TypeID == ZkData.ResourceType.Map);
			if (map == null) throw new ApplicationException("Map name is unknown");
			var mod = db.Resources.SingleOrDefault(x => x.InternalName == mission.Mod && x.TypeID == ZkData.ResourceType.Mod);
			if (mod == null) throw new ApplicationException("Mod name is unknown");
			if (db.Resources.Any(x => x.InternalName == mission.Name && x.MissionID != mission.MissionID)) throw new ApplicationException("Name already taken by other mod/map");

			var prev = db.Missions.Where(x => x.MissionID == mission.MissionID).SingleOrDefault();

			if (prev != null)
			{
				if (prev.AccountID != acc.AccountID && !acc.IsAdmin) throw new ApplicationException("Invalid author or password");
				prev.Description = mission.Description;
				prev.Mod = mission.Mod;
				prev.Map = mission.Map;
				prev.Name = mission.Name;
				prev.ScoringMethod = mission.ScoringMethod;
				prev.Script = mission.Script;
				prev.ModRapidTag = mission.ModRapidTag;
				prev.ModOptions = mission.ModOptions;
				prev.Image = mission.Image;
				prev.MissionEditorVersion = mission.MissionEditorVersion;
				prev.SpringVersion = mission.SpringVersion;
				prev.Revision++;
				prev.Mutator = mission.Mutator;
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
			mission.MinHumans = slots.Count(x => x.IsHuman && x.IsRequired);
			mission.MaxHumans = slots.Count(x => x.IsHuman);
			mission.ModifiedTime = DateTime.UtcNow;
			mission.IsDeleted = true;
			mission.IsCoop = slots.Where(x => x.IsHuman).GroupBy(x => x.AllyID).Count() == 1;

			db.SubmitChanges();

			db.Resources.DeleteAllOnSubmit(db.Resources.Where(x => x.MissionID == mission.MissionID));
			db.SubmitChanges();

			var resource = db.Resources.FirstOrDefault(x => x.InternalName == mission.Name); // todo delete full resource data
			if (resource == null)
			{
				resource = new Resource() { InternalName = mission.Name, DownloadCount = 0, TypeID = ZkData.ResourceType.Mod };
				db.Resources.InsertOnSubmit(resource);
			}
			resource.MissionID = mission.MissionID;

			resource.ResourceDependencies.Clear();
			resource.ResourceDependencies.Add(new ResourceDependency() { NeedsInternalName = map.InternalName });
			resource.ResourceDependencies.Add(new ResourceDependency() { NeedsInternalName = mod.InternalName });
			resource.ResourceContentFiles.Clear();

			

			// generate torrent
			var tempFile = Path.Combine(Path.GetTempPath(), mission.SanitizedFileName);
			File.WriteAllBytes(tempFile, mission.Mutator.ToArray());
			var creator = new TorrentCreator();
			creator.Path = tempFile;
			var torrentStream = new MemoryStream();
			creator.Create(torrentStream);
			try
			{
				File.Delete(tempFile);
			}
			catch {}

			var md5 = Hash.HashBytes(mission.Mutator.ToArray()).ToString();
			resource.ResourceContentFiles.Add(new ResourceContentFile()
			                                  {
			                                  	FileName = mission.SanitizedFileName,
			                                  	Length = mission.Mutator.Length,
			                                  	LinkCount = 1,
			                                  	Links = string.Format(MissionFileUrl, mission.Name),
			                                  	Md5 = md5
			                                  });

			var sh = resource.ResourceSpringHashes.SingleOrDefault(x => x.SpringVersion == mission.SpringVersion);
			if (sh == null)
			{
				sh = new ResourceSpringHash();
				resource.ResourceSpringHashes.Add(sh);
			}
			sh.SpringVersion = mission.SpringVersion;
			sh.SpringHash = modInfo.Checksum;

			modInfo.MissionMap = mission.Map; // todo solve properly - it should be in mod and unitsync should be able to read it too


		  var basePath = ConfigurationManager.AppSettings["ResourcePath"]  ?? @"d:\zero-k.info\www\resources\";
      File.WriteAllBytes(string.Format(@"{2}\{0}_{1}.torrent", mission.Name.EscapePath(), md5, basePath), torrentStream.ToArray());
			File.WriteAllBytes(string.Format(@"{1}\{0}.metadata.xml.gz", mission.Name.EscapePath(), basePath),
			                   MetaDataCache.SerializeAndCompressMetaData(modInfo));
			File.WriteAllBytes(string.Format(@"d:\zero-k.info\www\img\missions\{0}.png", mission.MissionID, basePath), mission.Image.ToArray());

			mission.IsDeleted = false;
			db.SubmitChanges();
		}
	}
}