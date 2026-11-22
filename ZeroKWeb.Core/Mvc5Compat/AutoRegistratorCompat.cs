using System;
using System.Collections.Generic;

using ZkData;

namespace AutoRegistrator
{
    /// <summary>
    /// The map registrar, as far as MapsController needs to name it - and no further.
    ///
    /// **This is a tripwire, not a shim.** The real AutoRegistrator P/Invokes into unitsync,
    /// the native Spring library that reads map and game archives, and pulls in MonoTorrent and
    /// PlasmaDownloader behind it. None of that is portable by writing C#; it needs the native
    /// library built for the target and a decision about where the registrar runs at all.
    ///
    /// Exactly one of MapsController's nine actions touches it - UploadResource - and the
    /// other eight, along with three of the four Maps views, have nothing to do with unitsync.
    /// So the controller is linked and that one action throws, loudly, on its first line:
    ///
    ///     Global.AutoRegistrator is not available on .NET 9 ...
    ///
    /// The alternative considered and rejected was a shim that returns an empty scan. It would
    /// compile identically, accept the upload, report "0 resources registered" and lose the
    /// file - which is the EntityFramework.Extensions trap this port has now hit three times.
    ///
    /// **This is also the same kind of coupling the Ladders work turned up**: the website
    /// reaches into infrastructure that happens to live in its own process. RatingSystems is
    /// the lobby server's; this is the registrar's. Neither shows up in a count of API calls.
    /// </summary>
    public class UnitSyncer
    {
        /// <summary>Field for field with the original's nested type, which the controller reads.</summary>
        public class ScanResult
        {
            public ResourceInfo ResourceInfo { get; set; }
            public ResourceFileStatus Status { get; set; }
        }

        /// <summary>
        /// The real enum's values, kept identical because the controller compares against two of
        /// them and writes Status.ToString() into the view.
        /// </summary>
        public enum ResourceFileStatus
        {
            AlreadyExists = 0,
            Registered = 1,
            RegistrationError = 2,
            Reregistered = 3
        }

        public List<ScanResult> Scan(ICollection<string> forceReregister = null) => throw AutoRegistratorCompat.Unavailable();
    }

    public class AutoRegistrator
    {
        /// <summary>
        /// The real Paths is ZkData.SpringPaths, and this is deliberately NOT that type. Linking
        /// the real one pulls in a second half of PlasmaShared's Utils partial for MakePath and
        /// GetAlternativeFileName, and declaring a fake ZkData.SpringPaths would put a hollow type
        /// under a real name where something might later link the genuine one on top of it.
        /// The property throws before anything reads a member, so all its type has to do is
        /// carry the name the controller spells - and say what it is.
        /// </summary>
        public sealed class PathsAreUnavailable
        {
            public string WritableDirectory => throw AutoRegistratorCompat.Unavailable();
        }

        public PathsAreUnavailable Paths => throw AutoRegistratorCompat.Unavailable();
        public UnitSyncer UnitSyncer => throw AutoRegistratorCompat.Unavailable();

        /// <summary>
        /// Mirrors the real signature so AdminController links. Note what it does NOT take: the
        /// real one reaches PlasmaDownloader for the archive, and keeping that inside the
        /// registrar rather than at the call site is what stops DownloadType having to exist here.
        /// </summary>
        public string ReregisterResource(string internalName, TimeSpan? downloadTimeout = null) =>
            throw AutoRegistratorCompat.Unavailable();
    }

    /// <summary>
    /// Same tripwire, for the Steam depot builder - EnginesController.MakeDefault calls
    /// RunAll() after changing the default engine. It lives in the AutoRegistrator project and
    /// reaches unitsync through it, so it is unportable for the same reason and in the same way.
    /// </summary>
    public class SteamDepotGenerator
    {
        public void RunAll() => throw AutoRegistratorCompat.Unavailable();
    }

    internal static class AutoRegistratorCompat
    {
        internal static NotSupportedException Unavailable() => new NotSupportedException(
            "The map registrar is not available on .NET 9: it P/Invokes into unitsync, and porting " +
            "it means building that native library for the target and deciding where the registrar " +
            "runs. Only MapsController.UploadResource needs it. See Mvc5Compat/AutoRegistratorCompat.cs.");
    }
}
