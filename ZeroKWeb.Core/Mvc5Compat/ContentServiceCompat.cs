using System;
using System.Collections.Generic;

namespace ZeroKWeb
{
    /// <summary>
    /// A tripwire standing where <c>Zero-K.info/ContentService.svc.cs</c> is, so
    /// <c>EnginesController</c> can be linked. One caller: <c>MakeDefault</c>.
    ///
    /// The real class implements <c>PlasmaShared.IContentService</c>, a WCF
    /// <c>[ServiceContract]</c>. .NET 9 has client-side WCF and no server side, so the interface
    /// does not compile here and neither does anything implementing it.
    ///
    /// **A shim returning an empty list would be worse than this.** MakeDefault reads
    /// <c>GetEngineList(null).Contains(engine)</c> and, on a miss, logs "Engine not found in the
    /// list" and carries on - so an empty list turns every attempt to change the default engine
    /// into a warning in a log nobody is reading, and the button appears to work.
    ///
    /// **MakeDefault could not work on the port anyway, and not only because of this.** It also
    /// calls <c>Global.SteamDepotGenerator.RunAll()</c>, which needs the registrar and unitsync,
    /// and <c>Global.LobbyApi.SetEngine</c>, which needs a lobby server. All three are
    /// out-of-process concerns; this is the Engines equivalent of MapsController.UploadResource.
    /// The other two actions, and Engines/EnginesIndex.cshtml, have nothing to do with any of it.
    /// </summary>
    public class ContentService
    {
        public List<string> GetEngineList(string platform) => throw Unavailable();

        internal static NotSupportedException Unavailable() => new NotSupportedException(
            "ContentService is not available on .NET 9: it implements a WCF ServiceContract, and " +
            ".NET 9 has no server-side WCF. Only EnginesController.MakeDefault needs it, and that " +
            "action needs the registrar and a lobby server as well. See " +
            "Mvc5Compat/ContentServiceCompat.cs.");
    }
}
