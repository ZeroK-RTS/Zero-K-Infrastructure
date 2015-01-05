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
        public List<SpringHashEntry> SpringHashes;
        public float? FeaturedOrder;

        public ResourceData() { }

    }
}