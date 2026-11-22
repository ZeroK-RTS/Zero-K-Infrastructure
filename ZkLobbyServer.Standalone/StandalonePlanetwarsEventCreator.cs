using System;
using LobbyClient;
using PlasmaShared;
using ZkData;

namespace ZkLobbyServer.Standalone
{
    /// <summary>
    /// PlanetWars events, written by a lobby server that is not inside the website.
    ///
    /// **This used to throw.** `IPlanetwarsEventCreator` was implemented once, in the website, and
    /// it formatted the event feed's HTML with the website's own helpers - so a standalone server
    /// could not write an event at all, and this class existed to say so rather than to stub it
    /// and quietly fill the feed with unlinked events.
    ///
    /// The formatting is now in ZkData/PlanetwarsEventFormatter.cs, which both halves call. What
    /// remains here is the two things only this process can supply.
    ///
    /// **URLs are absolute.** The website builds them with its own UrlHelper, which gives paths
    /// like /Clans/Detail/7 relative to whatever is serving. Nothing here is serving anything, so
    /// links are built on GlobalConst.BaseSiteUrl - the events are read on the website, and a
    /// root-relative link written by a process that is not the website would be a link to nowhere.
    ///
    /// **There is no viewer.** The website colours an account by the faction of whoever triggered
    /// the event, because the HTML is rendered once and stored. A server has no such person, so
    /// the context is left empty and the formatter falls back to its default colour - which is
    /// what the in-process server already produced, since it had no request either.
    /// </summary>
    public class StandalonePlanetwarsEventCreator : IPlanetwarsEventCreator
    {
        private ZkLobbyServer server;

        /// <summary>
        /// Set once the server exists. ServerRunner needs a creator to construct the server and the
        /// creator needs the server to speak through it, so one of them is late - and it is this
        /// one, because an event written before the server is up has nowhere to announce itself.
        /// </summary>
        public void Attach(ZkLobbyServer attached)
        {
            server = attached ?? throw new ArgumentNullException(nameof(attached));
        }

        public Event CreateEvent(string format, params object[] args) =>
            PlanetwarsEventFormatter.CreateEvent(Context(), Say, format, args);

        public void GhostPm(string user, string text) => Server().GhostPm(user, text);

        private void Say(Say say) => Server().GhostSay(say);

        /// <summary>
        /// Throws rather than doing nothing. A creator that silently dropped every channel
        /// announcement would look exactly like a working one, and the events would still appear
        /// on the site - so the only symptom would be a clan never hearing about its own planet.
        /// </summary>
        private ZkLobbyServer Server() => server
            ?? throw new InvalidOperationException(
                "the event creator has no lobby server yet - call Attach after ServerRunner is built");

        /// <summary>See the class note: absolute URLs, no viewer.</summary>
        private static ZkHtmlContext Context() => new ZkHtmlContext
        {
            Action = (action, controller, values) => GlobalConst.BaseSiteUrl
                                                     + "/" + controller + "/" + action + IdOf(values),
        };

        /// <summary>
        /// The site's default route is {controller}/{action}/{id}, and every call this formatter
        /// makes passes either nothing or a single id - so this covers all of them. It is written
        /// out rather than reflected over generally, because a route it cannot express should
        /// fail to compile here rather than produce a subtly wrong link.
        /// </summary>
        private static string IdOf(object values)
        {
            if (values == null) return "";
            var id = values.GetType().GetProperty("id");
            if (id == null) return "";
            return "/" + id.GetValue(values, null);
        }
    }
}
