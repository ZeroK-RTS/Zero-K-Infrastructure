#region using

using System;
using System.Configuration;
using System.Linq;
using System.Web;
using PlanetWarsShared;

#endregion

/// <summary>
/// Summary description for Globals
/// </summary>
public static class Globals
{
    static AuthInfo DefaultLogin = new AuthInfo("guest", "guest");

    /// <summary>
    /// Gets new or cached instance of IServer
    /// </summary>
    public static IServer Server
    {
        get
        {
            var app = HttpContext.Current.Application;
            if (app["IServer"] != null) {
                return (IServer)app["IServer"];
            } else {
                var iserver =
                    (IServer)Activator.GetObject(typeof (IServer), ConfigurationManager.AppSettings["PlanetWarsServer"]);
                app["IServer"] = iserver;
                return iserver;
            }
        }
    }

    /// <summary>
    /// Gets current user login information
    /// </summary>
    public static AuthInfo CurrentLogin
    {
        get { return (AuthInfo)HttpContext.Current.Session["loginInfo"]; }
        set { HttpContext.Current.Session["loginInfo"] = value; }
    }

    /// <summary>
    /// gets current player
    /// </summary>
    public static Player Player
    {
        get
        {
            if (Galaxy != null && CurrentLogin != null) {
                return Galaxy.Players.SingleOrDefault(p => p.Name == CurrentLogin.Login);
            }
            return null;
        }
    }

    public static string MyFaction
    {
        get
        {
            if (Player != null) {
                return Player.FactionName;
            } else {
                return null;
            }
        }
    }

    public static string MyPlayerName
    {
        get
        {
            if (Player != null) {
                return Player.Name;
            } else {
                return null;
            }
        }
    }

    /// <summary>
    /// gets current galaxy
    /// </summary>
    public static Galaxy Galaxy
    {
        get
        {
            if (Server == null) return null;
            var lastChanged = (DateTime?)HttpContext.Current.Application["lastChanged"]; 
            var stored = HttpContext.Current.Application["galaxy"] as Galaxy;
            var asked = HttpContext.Current.Items["asked"]; // this is tored in the context of one displayed page
            HttpContext.Current.Items["asked"] = true;
            if (stored != null && (asked != null || (lastChanged.HasValue && lastChanged.Value == Server.LastChanged))) {
                return stored;
            }
            HttpContext.Current.Application["lastChanged"] = Server.LastChanged;
            var gal = CurrentLogin == null
                                  ? Server.GetGalaxyMap(DefaultLogin)
                                  : Server.GetGalaxyMap(CurrentLogin);
            if (gal == null) // galaxy login failed apparently, try again with default
            {
                Server.GetGalaxyMap(DefaultLogin);
                CurrentLogin = null;
            }
            HttpContext.Current.Application["galaxy"] = gal;
            return gal;
        }
    }
    

    public static void ResetGalaxy()
    {
        HttpContext.Current.Items["asked"] = null;
    }

    public static bool TryLogin(string login, string password)
    {
        var auth = new AuthInfo(login, password);
        var gal = Server.GetGalaxyMap(auth);
        if (gal != null) {
            CurrentLogin = auth;
            return true;
        }
        return false;
    }
}