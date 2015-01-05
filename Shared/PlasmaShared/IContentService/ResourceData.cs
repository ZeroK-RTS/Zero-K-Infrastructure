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
        private int? MapFFAMaxTeams;
        private bool? MapIs1v1;
        private bool? MapIsFfa;
        private bool? MapIsSpecial;
        private bool? MapIsChickens;


        public ResourceData() { }

        /* HACK implement public ResourceData(Resource r)
        {
            ResourceID = r.ResourceID;
            InternalName = r.InternalName;
            ResourceType = r.TypeID;
            Dependencies = r.ResourceDependencies.Select(x => x.NeedsInternalName).ToList();
            SpringHashes =
                r.ResourceSpringHashes.Select(x => new SpringHashEntry { SpringHash = x.SpringHash, SpringVersion = x.SpringVersion }).ToList();
            FeaturedOrder = r.FeaturedOrder;
            MapFFAMaxTeams = r.MapFFAMaxTeams;
            MapIs1v1 = r.MapIs1v1;
            MapIsFfa = r.MapIsFfa;
            MapIsSpecial = r.MapIsSpecial;
            MapIsChickens = r.MapIsChickens;
        }*/
    }
}