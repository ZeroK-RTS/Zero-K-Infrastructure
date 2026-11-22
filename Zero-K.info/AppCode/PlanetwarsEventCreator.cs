using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using JetBrains.Annotations;
using LobbyClient;
using ZeroKWeb;
using ZkData;


public class PlanetwarsEventCreator:IPlanetwarsEventCreator {
    /// <summary>
    /// Delegates to ZkData/PlanetwarsEventFormatter.cs, which is the one implementation - the
    /// lobby server needs it too and cannot reach into this assembly.
    ///
    /// The context and the say delegate are what this half supplies: the website's per-request
    /// viewer and URL helper, and GhostSay through whichever lobby server it is talking to. Both
    /// are exactly what the body used to read off Global, so events are formatted as before.
    /// </summary>
    [StringFormatMethod("format")]
    public static Event CreateEvent(string format, params object[] args)
    {
        return PlanetwarsEventFormatter.CreateEvent(
            new ZkHtmlContext
            {
                Action = (action, controller, values) => Global.UrlHelper().Action(action, controller, values),
                ViewerFactionID = Global.FactionID,
                ViewerClanID = Global.ClanID,
                ViewerIsModerator = Global.IsModerator,
            },
            Global.LobbyApi == null ? (Action<Say>)null : say => Global.LobbyApi.GhostSay(say),
            format,
            args);
    }

    Event IPlanetwarsEventCreator.CreateEvent(string format, params object[] args)
    {
        return CreateEvent(format, args);
    }

    public void GhostPm(string user, string text)
    {
        Global.LobbyApi.GhostPm(user, text);
    }

}