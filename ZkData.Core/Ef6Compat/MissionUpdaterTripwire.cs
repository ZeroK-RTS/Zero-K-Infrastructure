using System;
using ZkData.UnitSyncLib;

namespace ZkData
{
    /// <summary>
    /// **A tripwire, not a shim**, in the shape AutoRegistrator established.
    ///
    /// The real MissionUpdater (ZkData/MissionService/MissionUpdater.cs) rewrites the mission
    /// archive when a mission is published, and to do that it needs two things this port does not
    /// have: **unitsync**, the native Spring library, and **MonoTorrent**, the vendored .NET
    /// Framework project in Shared/MonoTorrent that makes the mission's torrent. Neither had been
    /// listed as a .NET 9 blocker until MissionServiceController needed them.
    ///
    /// So the file is not linked and this stands in its place. MissionServiceLogic - the one
    /// implementation behind both /MissionService and MissionService.svc - compiles, and with it
    /// the controller and MissionServiceImplementation. Exactly one operation reaches this:
    /// SendMission, which publishes. GetMission, GetMissionByID, ListMissionInfos, DeleteMission
    /// and UndeleteMission never touch it.
    ///
    /// Linking the real file instead was tried first and is the wrong trade: it would mean writing
    /// a fake MonoTorrent API surface (TorrentCreator.Path, .Create, Hash) for the compiler, which
    /// is the "compiles and does nothing" failure this port refuses, at the scale of a library.
    /// </summary>
    public class MissionUpdater
    {
        public void UpdateMission(ZkDataContext db, Mission mission, Mod modInfo)
        {
            throw new NotSupportedException(
                "Publishing a mission is not available on .NET 9 yet: MissionUpdater rewrites the " +
                "mission archive using unitsync (native) and MonoTorrent (Shared/MonoTorrent, not " +
                "ported). Reading, listing and deleting missions do not need either.");
        }
    }
}
