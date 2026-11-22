using System;
using System.Collections.Generic;
using PlasmaShared;
using ZkData.UnitSyncLib;

namespace ZkData
{
    /// <summary>
    /// <see cref="IMissionService"/>'s six operations as JSON messages, so the mission editor can
    /// stop needing WCF.
    ///
    /// **Why this exists.** Server-side WCF has no in-box successor on .NET 9, and
    /// `MissionService.svc` is one of the two endpoints that made the port's WCF step
    /// "could not be carried out as written". Both ends of this one are in this repository, so it
    /// can move without waiting for anyone - where `ContentService.svc`'s remaining callers are
    /// clients deployed years ago, which is why that one had to be made to report itself (see
    /// `Zero-K.info/AppCode/LegacyCallReporter.cs`) before it can be retired.
    ///
    /// **The contract does not change, only the envelope.** `Mission` is `[DataContract]` with
    /// explicit `[DataMember]` fields, and Json.NET honours that, so exactly the same fields cross
    /// as under WCF - navigations the entity carries are not among them, then or now.
    /// `MissionSlot` and `Mod` are `[Serializable]` with public fields only, which both
    /// serialisers treat the same way. MissionServiceRoundTripTests checks that rather than
    /// trusting it.
    ///
    /// The shape - an ApiRequest/ApiResponse pair per operation, dispatched by class name through
    /// CommandJsonSerializer - is the one `/ContentService` already uses. A second convention for
    /// the same job would be worse than either.
    /// </summary>
    /// <summary>
    /// The message types, listed rather than discovered.
    ///
    /// `Utils.GetAllTypesWithAttribute` scans the attribute's own assembly plus the entry,
    /// executing and calling ones - which is why `/ContentService`'s messages work: they live in
    /// PlasmaShared, where the attribute is. These cannot: they carry `Mission`, and PlasmaShared
    /// is built before ZkData and cannot reference it.
    ///
    /// So whoever builds a serializer for these adds them. Getting that wrong is not subtle - the
    /// serializer answers "Invalid json type" and nothing moves at all.
    /// </summary>
    public static class MissionServiceApi
    {
        public static IEnumerable<Type> MessageTypes => new[]
        {
            typeof(DeleteMissionRequest), typeof(DeleteMissionResponse),
            typeof(GetMissionRequest), typeof(GetMissionResponse),
            typeof(ListMissionInfosRequest), typeof(ListMissionInfosResponse),
            typeof(SendMissionRequest), typeof(SendMissionResponse),
        };
    }

    public class DeleteMissionRequest : ApiRequest<DeleteMissionResponse>
    {
        public int MissionID;
        public string Author;
        public string Password;
        /// <summary>False undeletes, which used to be a separate operation with the same body.</summary>
        public bool Delete = true;
    }

    public class DeleteMissionResponse : ApiResponse
    {
        /// <summary>Null when it worked. The WCF version threw ApplicationException instead.</summary>
        public string Error;
    }

    public class GetMissionRequest : ApiRequest<GetMissionResponse>
    {
        /// <summary>One of these is set; MissionID wins when both are.</summary>
        public string MissionName;
        public int? MissionID;
    }

    public class GetMissionResponse : ApiResponse
    {
        public Mission Mission;
    }

    public class ListMissionInfosRequest : ApiRequest<ListMissionInfosResponse>
    {
    }

    public class ListMissionInfosResponse : ApiResponse
    {
        /// <summary>Mutator, Script and Image are blanked, as the WCF operation blanked them.</summary>
        public List<Mission> Missions = new List<Mission>();
    }

    public class SendMissionRequest : ApiRequest<SendMissionResponse>
    {
        public Mission Mission;
        public List<MissionSlot> Slots = new List<MissionSlot>();
        public string Author;
        public string Password;
        public Mod ModInfo;
    }

    public class SendMissionResponse : ApiResponse
    {
        public string Error;
    }
}
