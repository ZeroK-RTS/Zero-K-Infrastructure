using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;

namespace ZeroKWeb
{
    /// <summary>
    /// MVC 5's <c>HttpContext.Application</c>, which MapsController uses three times as a
    /// process-wide cache of deserialised map metadata, keyed by resource id.
    ///
    /// ASP.NET Core has no Application state. The faithful equivalent is a process-wide
    /// dictionary with no expiry and no eviction, which is exactly what
    /// <c>HttpApplicationState</c> was - so that is what this is, rather than IMemoryCache,
    /// which would quietly start evicting entries under memory pressure and change how often
    /// the site goes back to disk.
    ///
    /// **Unbounded, on both stacks.** One entry per map resource ever viewed, never released.
    /// That is the behaviour MVC 5 has today; it is carried over rather than fixed, because a
    /// port that changes caching lifetimes while it is changing frameworks cannot tell which
    /// of the two caused a difference. Worth revisiting once the port is done.
    ///
    /// The MVC 5 twin is in Zero-K.info/AppCode/ViewContextCompat.cs, and the call sites moved
    /// from <c>HttpContext.Application[key]</c> to <c>this.ApplicationState()[key]</c> because
    /// C# has no extension properties - the same wall as Server.MapPath and Request.Params.
    /// </summary>
    public sealed class Mvc5ApplicationState
    {
        private static readonly ConcurrentDictionary<string, object> entries =
            new ConcurrentDictionary<string, object>();

        public object this[string key]
        {
            get => entries.TryGetValue(key, out var value) ? value : null;
            set => entries[key] = value;
        }
    }

    public static class ApplicationStateCompat
    {
        private static readonly Mvc5ApplicationState state = new Mvc5ApplicationState();

        public static Mvc5ApplicationState ApplicationState(this Controller controller) => state;
    }
}
