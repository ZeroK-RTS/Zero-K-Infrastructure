using System;

namespace PlasmaShared.UnitSyncLib
{
    [Serializable]
    public class UnitInfo: ICloneable
    {
        public string FullName { get; set; }
        public string Name { get; set; }

        public UnitInfo() {}

        public UnitInfo(string name, string fullName)
        {
            Name = name;
            FullName = fullName;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}