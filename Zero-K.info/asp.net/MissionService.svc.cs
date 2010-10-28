using System;
using System.Collections.Generic;
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
			var auth = new AuthServiceClient();
			var prev = db.Missions.Where(x => x.MissionID == missionID).SingleOrDefault();
			if (prev != null)
			{
				var acc = auth.VerifyAccount(author, password);
				if (acc == null) throw new ApplicationException("Invalid login name or password");
				if (acc.AccountID != prev.AccountID && !acc.IsLobbyAdministrator) throw new ApplicationException("You cannot delete a mission from another user");
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


		public void SendMission(Mission mission, List<MissionSlot> slots, string author, string password)
		{
			Account acc = null;
			var db = new ZkDataContext();
			if (Debugger.IsAttached) acc = db.Accounts.SingleOrDefault(x => x.Name == "Testor303");
			else acc = new AuthServiceClient().VerifyAccount(author, password);

			if (acc == null) throw new ApplicationException("Cannot verify user account");

			if (db.Missions.Any(x => x.Name == mission.Name)) throw new ApplicationException("Mission name must be unique");
			var map = db.Resources.SingleOrDefault(x => x.InternalName == mission.Map && x.TypeID == ZkData.ResourceType.Map);
			if (map == null) throw new ApplicationException("Map name is unknown");
			var mod = db.Resources.SingleOrDefault(x => x.InternalName == mission.Mod && x.TypeID == ZkData.ResourceType.Mod);
			if (mod == null) throw new ApplicationException("Mod name is unknown");
			if (db.Resources.Any(x=>x.InternalName == mission.Name && x.MissionID != mission.MissionID)) throw new ApplicationException("Name already taken by other mod/map");

			var prev = db.Missions.Where(x => x.MissionID == mission.MissionID).SingleOrDefault();

			if (prev != null)
			{
				if (prev.AccountID != acc.AccountID && !acc.IsLobbyAdministrator) throw new ApplicationException("Invalid author or password");

				db.Missions.Attach(mission, prev);

				mission.Revision++;
			}
			else
			{
				mission.AccountID = acc.AccountID;
				mission.CreatedTime = DateTime.UtcNow;
				db.Missions.InsertOnSubmit(mission);
			}
			mission.MinHumans = slots.Count(x => x.IsHuman && x.IsRequired);
			mission.MaxHumans = slots.Count(x => x.IsHuman);
			mission.ModifiedTime = DateTime.UtcNow;

			db.SubmitChanges();

			var resource = db.Resources.FirstOrDefault(x => x.InternalName == mission.Name);
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
			var modInfo = new Mod()
			              {
			              	ArchiveName = mission.SanitizedFileName,
			              	Name = mission.Name,
			              	Desctiption = mission.Description,
			              	Dependencies = new[] { mod.InternalName },
			              	MissionScript = mission.Script,
			              	MissionSlots = slots
			              };

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

			File.WriteAllBytes(string.Format(@"d:\PlasmaServer\Resources\{0}_{1}.torrent", mission.Name.EscapePath(), md5), torrentStream.ToArray());
			File.WriteAllBytes(string.Format(@"d:\PlasmaServer\Resources\{0}.metadata.xml.gz", mission.Name.EscapePath()),
			                   MetaDataCache.SerializeAndCompressMetaData(modInfo));

			db.SubmitChanges();
		}
	}
}