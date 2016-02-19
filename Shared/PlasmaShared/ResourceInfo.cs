using System.Collections.Generic;
using System.Xml.Serialization;
using ZkData.UnitSyncLib;

namespace ZkData
{
    [XmlInclude(typeof(Map))]
    [XmlInclude(typeof(Mod))]
    public class ResourceInfo
    {
        public string Author { get; set; }
        public string ArchiveName { get; set; }
        public string Name { get; set; }
        public List<string> Dependencies { get; set; } = new List<string>();
        public string Description { get; set; }
        public string Game { get; set; }
        public string ShortGame { get; set; }
        public string ShortName { get; set; }
        public string PrimaryModVersion { get; set; }
        public uint CheckSum { get; set; }
        public int ModType { get; set; }
        public string Mutator { get; set; }
        public string ArchivePath { get; set; }

        public virtual ResourceType ResourceType { get; } = ResourceType.Helper;

        public void FillTo(ResourceInfo target) {
            target.Author = Author;
            target.ArchiveName = ArchiveName;
            target.Name = Name;
            target.Dependencies = Dependencies;
            target.Description = Description;
            target.Game = Game;
            target.ShortGame = ShortGame;
            target.ShortName = ShortName;
            target.PrimaryModVersion = PrimaryModVersion;
            target.CheckSum = CheckSum;
            target.ModType = ModType;
            target.Mutator = Mutator;
            target.ArchivePath = ArchivePath;
        }
    }
}