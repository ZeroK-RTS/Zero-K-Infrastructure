using System.Collections.Generic;

namespace PlasmaShared.UnitSyncLib
{
    public class ArchiveEntry
    {
        public string InternalName;
        public string FileName;
        public string FilePath;
        public string Author;
        public List<string> Dependencies = new List<string>();
    }
}