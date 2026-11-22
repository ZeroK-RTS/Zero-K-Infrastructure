//because SharpCompress fails here for some reason

using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO.Compression;
using MonoTorrent.Common;
using PlasmaShared;
using ZkData.UnitSyncLib;

namespace ZkData
{
    public class MissionUpdater
    {
        static readonly string MissionFileUrl = GlobalConst.BaseSiteUrl + "/Missions/File/{0}";

        string GetModInfo(string missionNameWithVersion, string modRapidTag, string nameWithoutVersion, string modName, string shortName)
        {
            const bool hideFromModList = true;
            var sb = new StringBuilder();
            sb.AppendLine("local modinfo = {");
            sb.AppendFormat("  name          =	[[{0}]],\n", missionNameWithVersion);
            sb.AppendFormat("  description   =	[[{0}]],\n", "Mission Mutator"); // the real description might break archivecache.lua
            sb.AppendFormat("  modtype       =	[[{0}]],\n", hideFromModList ? 0 : 1);
            sb.AppendFormat("  shortname     =	[[{0}]],\n", shortName);
            /*sb.AppendFormat("  shortgame     =	[[{0}]],\n", mod.ShortGame);*/
            //sb.AppendFormat("  shortbasename =	[[{0}]],\n", mod.ShortBaseName);
            sb.AppendLine("  depend = {");
            sb.AppendLine("    " + (!string.IsNullOrEmpty(modRapidTag) ? System.String.Format("[[rapid://{0}]]\n", modRapidTag) : System.String.Format("[[{0}]]", modName)));
            sb.AppendLine("  },");
            sb.AppendLine("}");
            sb.AppendLine("return modinfo");
            return sb.ToString();
        }

        static void WriteZipArchiveEntry(ZipArchiveEntry entry, string toWrite)
        {
            using (StreamWriter writer = new StreamWriter(entry.Open()))
            {
                writer.Write(toWrite);
            }
        }

        public void UpdateMission(ZkDataContext db, Mission mission, SpringPaths paths, string engine)
        {
            var file = mission.Mutator.ToArray();
            var targetPath = Path.Combine(paths.WritableDirectory, "games", mission.SanitizedFileName);
            File.WriteAllBytes(targetPath, file);

            Mod modInfo;
            using (var unitsync = new UnitSync(paths, engine))
            {
                modInfo = unitsync.GetResourceFromFileName(targetPath) as Mod;
            }

            File.Delete(targetPath);
            UpdateMission(db, mission, modInfo);
        }

        public void UpdateMission(ZkDataContext db, Mission mission, Mod modInfo) {
            var file = mission.Mutator.ToArray();
            var tempName = Path.GetTempFileName() + ".zip";
            File.WriteAllBytes(tempName, file);

            

            using (var zf = ZipFile.Open(tempName, ZipArchiveMode.Update))
            {
                var modinfoEntry = zf.GetEntry("modinfo.lua");
                modinfoEntry.Delete();
                modinfoEntry = zf.CreateEntry("modinfo.lua");
                WriteZipArchiveEntry(modinfoEntry, GetModInfo(mission.NameWithVersion, mission.ModRapidTag, mission.Name, mission.Mod, "ZK"));
                FixScript(mission, zf, "script.txt");
                var script = FixScript(mission, zf, GlobalConst.MissionScriptFileName);
                modInfo.MissionScript = script;
                //modInfo.ShortName = mission.Name;
                modInfo.Name = mission.NameWithVersion;
            }
            mission.Mutator = File.ReadAllBytes(tempName);
            if (string.IsNullOrEmpty(mission.Script)) mission.Script = " "; // tweak for silly update validation
            mission.Script = Regex.Replace(mission.Script, "GameType=([^;]+);", (m) => $"GameType={mission.NameWithVersion};");
            
            File.Delete(tempName);
            
            var resource = db.Resources.FirstOrDefault(x => x.MissionID == mission.MissionID); 
            if (resource == null)
            {
                resource = new Resource() { DownloadCount = 0, TypeID = ZkData.ResourceType.Mod };
                db.Resources.Add(resource);
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


            var basePath = Path.Combine(GlobalConst.SiteDiskPath, GlobalConst.ResourceFolder);
            if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);
            File.WriteAllBytes(Path.Combine(basePath, $"{resource.InternalName.EscapePath()}_{md5}.torrent"), torrentStream.ToArray());
            
            File.WriteAllBytes(Path.Combine(basePath, $"{resource.InternalName.EscapePath()}.metadata.xml.gz"),
                                   MetaDataCache.SerializeAndCompressMetaData(modInfo));
            
            var imgPath = Path.GetDirectoryName(GetMissionImagePath(mission.MissionID));
            if (!Directory.Exists(imgPath)) Directory.CreateDirectory(imgPath);
            
            File.WriteAllBytes(GetMissionImagePath(mission.MissionID), mission.Image.ToArray());
        }

        /// <summary>
        /// Where a mission's image goes, and the only place that path is built.
        ///
        /// It is served back as {BaseSiteUrl}/img/missions/{id}.png - by
        /// ContentServiceImplementation to the game client, and by Missions/Detail.cshtml and
        /// TileList.cshtml as ~/img/missions/{id}.png - so the directory here and the URL there
        /// have to agree, and SiteDiskPathTests asks whether they still do.
        ///
        /// This used to be `imgPath + "{0}.png"` over a variable that ended in a separator. When
        /// that variable became a Path.Combine it stopped ending in one, and the image was written
        /// to img/missions123.png - beside the directory rather than in it, with the directory
        /// still created and nothing failing. Hence one definition and no concatenation.
        /// </summary>
        public static string GetMissionImagePath(int missionID)
        {
            return Path.Combine(GlobalConst.SiteDiskPath, "img", "missions", missionID + ".png");
        }

        static string FixScript(Mission mission, ZipArchive archive, string scriptName)
        {
            string script;
            ZipArchiveEntry entry = archive.GetEntry(scriptName);
            using (StreamReader reader = new StreamReader(entry.Open()))
            {
                script = reader.ReadToEnd();
                script = Regex.Replace(script, "GameType=([^;]+);", (m) => { return string.Format("GameType={0};", mission.NameWithVersion); });
            }
            WriteZipArchiveEntry(entry, script);
            return script;
        }
    }


    
}
