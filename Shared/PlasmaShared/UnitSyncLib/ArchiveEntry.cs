using System.Collections.Generic;

namespace PlasmaShared.UnitSyncLib
{
    public class ArchiveEntry
    {
        public string InternalName;
        public string FileName;
        public string FilePath;
        public string Author;
        public uint CheckSum;
        public List<string> Dependencies = new List<string>();
        public string Description;
        public int ModType;
        public string Mutator;
    }
}