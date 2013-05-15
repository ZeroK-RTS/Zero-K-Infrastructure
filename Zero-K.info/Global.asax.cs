using System;
using System.Collections.Generic;
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

        const string DbListKey = "ZkDataContextList";

        string GetUserIP()
        {
            var ip = Context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (string.IsNullOrEmpty(ip) || ip.Equals("unknown", StringComparison.OrdinalIgnoreCase)) ip = Context.Request.ServerVariables["REMOTE_ADDR"];
            return ip;
        }

        public MvcApplication()
        {
            ZkDataContext.DataContextCreated += context => {
                if (HttpContext.Current != null)
                {
                    List<ZkDataContext> dbs = HttpContext.Current.Items[DbListKey] as List<ZkDataContext>;
                    if (dbs != null) dbs.Add(context);
                }
            };
            this.BeginRequest += (sender, args) => {
                HttpContext.Current.Items[DbListKey] = new List<ZkDataContext>();
            };
            this.EndRequest += (sender, args) => {
                List<ZkDataContext> dbs = HttpContext.Current.Items[DbListKey] as List<ZkDataContext>;
                if (dbs != null) {
                    foreach (var db in dbs) {
                        try
                        {
                            db.Dispose();
                        }
                        catch { };
                    }
                }
            };

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
            routes.MapRoute("RedeemCode", "Contributions/Redeem/{code}", new { controller = "Contributions", action = "Redeem", code = UrlParameter.Optional });

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
#if DEPLOY
            Global.Nightwatch.Start();
#endif

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
                var ip = GetUserIP();

                using (var db = new ZkDataContext())
                {
                    var penalty = Punishment.GetActivePunishment(acc != null? acc.AccountID : 0, ip, null, x=>x.BanSite,db);
                    if (penalty != null)
                    {
                        Response.Write("You are banned!\n");
                        Response.Write(string.Format("Ban expires: {0} UTC\n", penalty.BanExpires));
                        Response.Write(string.Format("Reason: {0}\n", penalty.Reason));
                        Response.End();
                    }
                }
                if (acc != null) HttpContext.Current.User = acc;
            }


            if (Request["weblobby"] != null) { // save weblobby info
                var cookie = new HttpCookie("weblobby", Request["weblobby"]);
                cookie.Expires = DateTime.MinValue;
                Response.SetCookie(cookie);
            }
        }
    }
}