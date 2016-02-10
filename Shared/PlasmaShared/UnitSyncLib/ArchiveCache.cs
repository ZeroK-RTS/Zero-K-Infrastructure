using System.Collections.Generic;
using System.IO;
using System.Linq;
using Neo.IronLua;

namespace PlasmaShared.UnitSyncLib
{
    public class ArchiveCache
    {
        public ArchiveCache(string unitsyncWritableFolder) {
            Archives = new List<ArchiveEntry>();

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

                        var newEntry = new ArchiveEntry
                        {
                            FileName = v.name,
                            FilePath = v.path,
                            InternalName = v.archivedata.name,
                            Author = v.archivedata.author,
                            CheckSum = v.checksum,
                            Description = v.description,
                            ModType = v.modtype,
                            Mutator = v.mutator
                        };

                        if (v.archivedata.depend != null) foreach (var dep in v.archivedata.depend) newEntry.Dependencies.Add(dep.Value);

                        Archives.Add(newEntry);
                    }
                }
            }
        }

        public List<ArchiveEntry> Archives { get; }
    }
}