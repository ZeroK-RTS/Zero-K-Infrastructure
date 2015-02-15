using System;

namespace ZkData.UnitSyncLib
{
    [Serializable]
    public class ListOption: ICloneable
    {
        public string Description;
        public string Key;
        public string Name;

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}