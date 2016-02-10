using System.Collections.Generic;
using System.IO;
using System.Linq;
using Neo.IronLua;
using ZkData;

namespace PlasmaShared.UnitSyncLib
{
    public class ArchiveCache
    {
        public ArchiveCache(string unitsyncWritableFolder) {
            Archives = new List<ResourceInfo>();

            var di = new DirectoryInfo(Path.Combine(unitsyncWritableFolder, "cache"));
            var fi = di.GetFiles("ArchiveCache*.lua").OrderByDescending(x => x.LastWriteTime).FirstOrDefault();
            if (fi != null)
            {
                var lua = new Lua();
                var luaEnv = lua.CreateEnvironment();
                using (var file = fi.OpenText())
                {
                    dynamic result = luaEnv.DoChunk(file, "dummy.lua");
                    foreach (var archive in result.archives)
                    {
                        var v = archive.Value;

                        if (v.archivedata != null)
                        {

                            var newEntry = new ResourceInfo()
                            {
                                ArchiveName = v.name,
                                ArchivePath = v.path,
                                Name = v.archivedata.name,
                                Author = v.archivedata.author,
                                Description = v.description,
                                Mutator = v.mutator,
                                ShortGame = v.shortgame,
                                Game = v.game,
                                ShortName = v.shortname,
                                PrimaryModVersion = v.version,
                            };
                            if (v.modtype != null) newEntry.ModType = v.modtype;
                            if (v.checksum != null)
                            {
                                uint temp;
                                if (uint.TryParse(v.checksum, out temp)) newEntry.CheckSum = temp;
                            }
                            if (v.archivedata.depend != null) foreach (var dep in v.archivedata.depend) newEntry.Dependencies.Add(dep.Value);

                            Archives.Add(newEntry);
                        }
                    }
                }
            }
        }

        public List<ResourceInfo> Archives { get; }
    }
}