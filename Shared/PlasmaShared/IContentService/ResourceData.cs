using System.Collections.Generic;
using ZkData;

namespace PlasmaShared
{
    public class ResourceData
    {
        public List<string> Dependencies;
        public string InternalName;
        public int ResourceID;
        public ResourceType ResourceType;
        public bool? MapIs1v1;
        public bool? MapIsTeams;
        public bool? MapIsFfa;
        public bool? MapIsSpecial;
        public bool? MapIsSupported;
        public float? FeaturedOrder;

        public ResourceData() { }

    }
}
