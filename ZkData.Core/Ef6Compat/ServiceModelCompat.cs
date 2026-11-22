using System;

namespace System.ServiceModel
{
    /// <summary>
    /// ZkData.IMissionService carries [ServiceContract] and [OperationContract], and
    /// System.ServiceModel does not exist on .NET 9 - server-side WCF has no successor there, which
    /// is the whole reason /MissionService exists beside MissionService.svc.
    ///
    /// These are inert metadata: nothing in the port reads them, and nothing can host WCF. Supplying
    /// them lets the interface and its one shared implementation compile unedited, which matters
    /// because MissionServiceLogic is the single implementation behind BOTH endpoints. Deleting the
    /// attributes from production would break the .svc that is still serving installed editors.
    ///
    /// They go away with the .svc itself - see the deprecation notice in MissionService.svc.cs.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ServiceContractAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class OperationContractAttribute : Attribute
    {
        public bool IsOneWay { get; set; }
    }
}
