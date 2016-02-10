using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo.IronLua;
using ZkData.UnitSyncLib;

namespace PlasmaShared.UnitSyncLib
{
    public class ArchiveCache
    {
        public List<ArchiveEntry> Archives { get; private set; }

        public ArchiveCache(string unitsyncWritableFolder) {
            Archives = new List<ArchiveEntry>();

            DirectoryInfo di = new DirectoryInfo(Path.Combine(unitsyncWritableFolder, "cache"));
            var fi = di.GetFiles("ArchiveCache*.lua").OrderByDescending(x => x.LastWriteTime).FirstOrDefault();
            if (fi != null)
            {
                var lua = new Lua();
                var luaEnv = lua.CreateEnvironment();
                using (var file = fi.OpenText())
                {
                    dynamic result = luaEnv.DoChunk(file, "dummy.lua");
                    foreach (dynamic archive in result.archives)
                    {
                        var v = archive.Value;
                        var newEntry = new ArchiveEntry()
                        {
                            FileName = v.name,
                            FilePath = v.path,
                            InternalName = v.archivedata.name,
                            Author = v.archivedata.author,
                            CheckSum = v.checksum
                        };

                        if (v.archivedata.depend != null)
                        {
                            foreach (dynamic dep in v.archivedata.depend)
                            {
                                newEntry.Dependencies.Add(dep.Value);
                            }
                        }

                        Archives.Add(newEntry);
                        
                    }
                }
            }
        }
    }
}
