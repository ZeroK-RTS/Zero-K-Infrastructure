using System;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using CaTracker;
using ZeroKWeb.Controllers;
using ZkData;

namespace ZeroKWeb
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication: HttpApplication
    {
        DateTime lastPollCheck = DateTime.UtcNow;

        public MvcApplication()
        {
            PostAuthenticateRequest += MvcApplication_PostAuthenticateRequest;
            Error += MvcApplication_Error;
        }


        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute("WikiPage", "Wiki/{node}", new { controller = "Wiki", action = "Index", node = UrlParameter.Optional });
            routes.MapRoute("WikiPage2", "p/zero-k/wiki/{node}", new { controller = "Wiki", action = "Index", node = UrlParameter.Optional });
            routes.MapRoute("MissionImage", "Missions/Img/{name}", new { controller = "Missions", action = "Img", name = UrlParameter.Optional });
            routes.MapRoute("MissionFile", "Missions/File/{name}", new { controller = "Missions", action = "File", name = UrlParameter.Optional });
            routes.MapRoute("StaticFile", "Static/{name}", new { controller = "Static", action = "Index", name = UrlParameter.Optional });

            routes.MapRoute("Default", "{controller}/{action}/{id}", new { controller = "Home", action = "Index", id = UrlParameter.Optional });

            routes.MapRoute("Root", "", new { controller = "Home", action = "Index", id = "" });
        }

        public override string GetVaryByCustomString(HttpContext context, string custom)
        {
            if (custom == GlobalConst.LobbyAccessCookieName) return Global.IsLobbyAccess.ToString();
            return base.GetVaryByCustomString(context, custom);
        }

        protected void Application_Start()
        {
            Application["Nightwatch"] = new Nightwatch(Server.MapPath("/"));
            if (!Debugger.IsAttached) Global.Nightwatch.Start();

            AreaRegistration.RegisterAllAreas();
            RegisterRoutes(RouteTable.Routes);
        }

        void MvcApplication_Error(object sender, EventArgs e)
        {
            if (Request.Url.ToString().Contains(".mvc")) Response.RedirectPermanent(Request.Url.ToString().Replace(".mvc", ""));
            else Server.GetLastError();
        }

        void MvcApplication_PostAuthenticateRequest(object sender, EventArgs e)
        {
            if (DateTime.UtcNow.Subtract(lastPollCheck).TotalMinutes > 15)
            {
                PollController.AutoClosePolls();
                lastPollCheck = DateTime.UtcNow;
            }

            if (Request[GlobalConst.LoginCookieName] != null)
            {
                var acc = AuthServiceClient.VerifyAccountHashed(Request[GlobalConst.LoginCookieName], Request[GlobalConst.PasswordHashCookieName]);
                if (acc != null)
                {
                    if (acc.Punishments.Any(x => x.BanExpires > DateTime.UtcNow && x.BanSite))
                    {
                        Response.Write("Banned!");
                        Response.End();
                    }
                    else HttpContext.Current.User = acc;
                }
            }
        }
    }
}