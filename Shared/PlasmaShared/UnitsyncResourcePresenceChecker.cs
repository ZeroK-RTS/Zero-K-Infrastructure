using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ZkData.UnitSyncLib;

namespace ZkData
{
    public class UnitsyncResourcePresenceChecker:IDisposable, IResourcePresenceChecker
    {
        private UnitSync unitsync;

        public UnitsyncResourcePresenceChecker(SpringPaths paths, string engine)
        {
            if (string.IsNullOrEmpty(engine) || !paths.HasEngineVersion(engine))
            {
                Trace.TraceWarning("Engine {0} not found, trying backup", engine);
                engine = paths.GetEngineList().FirstOrDefault();
                if (engine == null) throw new Exception("No engine found for unitsync");
            }
            unitsync = new UnitSync(paths, engine);
        }


        public void Dispose()
        {
            unitsync?.Dispose();
        }

        public bool HasResource(string internalName)
        {
            return unitsync.GetArchiveEntryByInternalName(internalName) != null;
        }
    }
}