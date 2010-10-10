#region using

using System.Collections.Generic;
using System.Linq;

#endregion

namespace PlasmaServer
{
    public class ResourceData
    {
        #region Properties

        public List<string> Dependencies;
        public string InternalName;
        public ResourceType ResourceType;
        public List<SpringHashEntry> SpringHashes;

        #endregion

        #region Constructors

        public ResourceData() {}

        public ResourceData(Resource r)
        {
            InternalName = r.InternalName;
            ResourceType = r.TypeID;
            Dependencies = r.Dependencies.Select(x => x.NeedsInternalName).ToList();
            SpringHashes =
                r.ResourceSpringHashes.Select(x => new SpringHashEntry {SpringHash = x.SpringHash, SpringVersion = x.SpringVersion}).ToList();
        }

        #endregion
    }

    public class SpringHashEntry
    {
        #region Properties

        public int SpringHash;
        public string SpringVersion;

        #endregion
    }
}