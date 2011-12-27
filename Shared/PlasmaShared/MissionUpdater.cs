using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Linq;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Ionic.Zip;
using MonoTorrent.Common;
using PlasmaShared;
using PlasmaShared.UnitSyncLib;
using ZkData;

namespace PlasmaShared
{
    public class MissionUpdater
    {
        const string MissionFileUrl = "http://zero-k.info/Missions/File/{0}";

        string GetModInfo(string missionNameWithVersion, string modName)
        {
            const bool hideFromModList = false;
            var sb = new StringBuilder();
            sb.AppendLine("local modinfo = {");
            sb.AppendFormat("  name          =	[[{0}]],\n", missionNameWithVersion);
            sb.AppendFormat("  description   =	[[{0}]],\n", "Mission Mutator"); // the real description might break archivecache.lua
            sb.AppendFormat("  modtype       =	[[{0}]],\n", hideFromModList ? 0 : 1);
            /*sb.AppendFormat("  shortname     =	[[{0}]],\n", mod.ShortName);
            sb.AppendFormat("  shortgame     =	[[{0}]],\n", mod.ShortGame);*/
            //sb.AppendFormat("  shortbasename =	[[{0}]],\n", mod.ShortBaseName);
            sb.AppendLine("  depend = {");
            sb.AppendFormat("    [[{0}]]\n", modName);
            sb.AppendLine("  },");
            sb.AppendLine("}");
            sb.AppendLine("return modinfo");
            return sb.ToString();
        }

        
        public void UpdateMission(ZkDataContext db, Mission mission, Mod modInfo) {
            var file = mission.Mutator.ToArray();
            var tempName = Path.GetTempFileName() + ".zip";
            File.WriteAllBytes(tempName, file);
            using (var zf = new ZipFile(tempName))
            {
                zf.UpdateEntry("modinfo.lua", Encoding.UTF8.GetBytes(GetModInfo(mission.NameWithVersion, mission.Mod)));
                FixScript(mission, zf, "script.txt");
                var script = FixScript(mission, zf, GlobalConst.MissionScriptFileName);
                modInfo.MissionScript = script;
                zf.Save();
            }
            mission.Mutator = new Binary(File.ReadAllBytes(tempName));
            
            File.Delete(tempName);
            
            var resource = db.Resources.FirstOrDefault(x => x.MissionID == mission.MissionID); 
            if (resource == null)
            {
                resource = new Resource() { DownloadCount = 0, TypeID = ZkData.ResourceType.Mod };
                db.Resources.InsertOnSubmit(resource);
            }
            resource.InternalName = mission.NameWithVersion;
            resource.MissionID = mission.MissionID;

            resource.ResourceDependencies.Clear();
            resource.ResourceDependencies.Add(new ResourceDependency() { NeedsInternalName = mission.Map });
            resource.ResourceDependencies.Add(new ResourceDependency() { NeedsInternalName = mission.Mod });
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
            catch { }

            var md5 = Hash.HashBytes(mission.Mutator.ToArray()).ToString();
            resource.ResourceContentFiles.Add(new ResourceContentFile()
            {
                FileName = mission.SanitizedFileName,
                Length = mission.Mutator.Length,
                LinkCount = 1,
                Links = string.Format(MissionFileUrl, mission.MissionID),
                Md5 = md5
            });

            var sh = resource.ResourceSpringHashes.SingleOrDefault(x => x.SpringVersion == mission.SpringVersion);
            if (sh == null)
            {
                sh = new ResourceSpringHash();
                resource.ResourceSpringHashes.Add(sh);
            }
            sh.SpringVersion = mission.SpringVersion;
            sh.SpringHash = 0;
            

            var basePath = ConfigurationManager.AppSettings["ResourcePath"] ?? @"c:\projekty\zero-k.info\www\resources\";
            File.WriteAllBytes(string.Format(@"{2}\{0}_{1}.torrent", resource.InternalName.EscapePath(), md5, basePath), torrentStream.ToArray());
            
            File.WriteAllBytes(string.Format(@"{1}\{0}.metadata.xml.gz", resource.InternalName.EscapePath(), basePath),
                                   MetaDataCache.SerializeAndCompressMetaData(modInfo));
            
            File.WriteAllBytes(string.Format(@"c:\projekty\zero-k.info\www\img\missions\{0}.png", mission.MissionID, basePath), mission.Image.ToArray());

        }

        static string FixScript(Mission mission, ZipFile zf, string scriptName)
        {
            var ms = new MemoryStream();
            zf[scriptName].Extract(ms);
            var script = Encoding.UTF8.GetString(ms.ToArray());
            script = Regex.Replace(script, "GameType=([^;]+);", (m) => { return string.Format("GameType={0};", mission.NameWithVersion); });
            zf.UpdateEntry(scriptName, Encoding.UTF8.GetBytes(script));
            return script;
        }
    }


    
}
