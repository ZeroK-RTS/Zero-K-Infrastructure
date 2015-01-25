using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using CaTracker;
using NightWatch;
using ZeroKWeb.Controllers;
using ZkData;
using System.Web.Optimization;

namespace ZeroKWeb
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication: HttpApplication
    {
        const string DbListKey = "ZkDataContextList";
        DateTime lastPollCheck = DateTime.UtcNow;

        public MvcApplication() {
            ZkDataContext.DataContextCreated += context =>
                {
                    if (HttpContext.Current != null) {
                        var dbs = HttpContext.Current.Items[DbListKey] as List<ZkDataContext>;
                        if (dbs != null) dbs.Add(context);
                    }
                };
            BeginRequest += (sender, args) => { HttpContext.Current.Items[DbListKey] = new List<ZkDataContext>(); };
            EndRequest += (sender, args) =>
                {
                    var dbs = HttpContext.Current.Items[DbListKey] as List<ZkDataContext>;
                    if (dbs != null) {
                        foreach (var db in dbs) {
                            try {
                                db.Dispose();
                            } catch {}
                            ;
                        }
                    }
                };

            PostAuthenticateRequest += MvcApplication_PostAuthenticateRequest;
            PostAcquireRequestState += OnPostAcquireRequestState;
            Error += MvcApplication_Error;
        }


        public static void RegisterRoutes(RouteCollection routes) {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("Resources/{*pathInfo}");
            routes.IgnoreRoute("img/{*pathInfo}");
            routes.IgnoreRoute("robots.txt");

            routes.MapRoute("WikiPage", "Wiki/{node}", new { controller = "Wiki", action = "Index", node = UrlParameter.Optional });
            routes.MapRoute("WikiPage2", "p/zero-k/wiki/{node}", new { controller = "Wiki", action = "Index", node = UrlParameter.Optional });
            routes.MapRoute("MissionImage", "Missions/Img/{name}", new { controller = "Missions", action = "Img", name = UrlParameter.Optional });
            routes.MapRoute("MissionFile", "Missions/File/{name}", new { controller = "Missions", action = "File", name = UrlParameter.Optional });
            routes.MapRoute("ReplayFile", "Replays/{name}", new { controller = "Replays", action = "Download", name = UrlParameter.Optional });

            routes.MapRoute("StaticFile", "Static/{name}", new { controller = "Static", action = "Index", name = UrlParameter.Optional });
            routes.MapRoute("RedeemCode",
                            "Contributions/Redeem/{code}",
                            new { controller = "Contributions", action = "Redeem", code = UrlParameter.Optional });

            routes.MapRoute("Default", "{controller}/{action}/{id}", new { controller = "Home", action = "Index", id = UrlParameter.Optional });

            routes.MapRoute("Root", "", new { controller = "Home", action = "Index", id = "" });
        }

        public override string GetVaryByCustomString(HttpContext context, string custom) {
            if (custom == GlobalConst.LobbyAccessCookieName) return Global.IsLobbyAccess.ToString();
            return base.GetVaryByCustomString(context, custom);
        }

        protected void Application_Start()
        {
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            var nw = new Nightwatch();
            Application["Nightwatch"] = nw;
            if (GlobalConst.PlanetWarsMode == PlanetWarsModes.Running) Application["PwMatchMaker"] = new PlanetWarsMatchMaker(nw.Tas);            
            Global.Nightwatch.Start();

            AreaRegistration.RegisterAllAreas();
            RegisterRoutes(RouteTable.Routes);
        }

        string GetUserIP() {
            var ip = Context.Request.ServerVariables["REMOTE_ADDR"];
            return ip;
        }

        void MvcApplication_Error(object sender, EventArgs e) {
            Exception ex = Context.Server.GetLastError();
            if (!ex.Message.Contains("was not found or does not implement IController")) Trace.TraceError(ex.ToString());
            //var context = HttpContext.Current;
            //context.Server.ClearError();
        }

        void MvcApplication_PostAuthenticateRequest(object sender, EventArgs e) {
            if (DateTime.UtcNow.Subtract(lastPollCheck).TotalMinutes > 15) {
                PollController.AutoClosePolls();
                lastPollCheck = DateTime.UtcNow;
            }

            Account acc = null;
            if (Request[GlobalConst.ASmallCakeCookieName] != null)
            {
                var testAcc = Account.AccountByName(new ZkDataContext(), Request[GlobalConst.ASmallCakeLoginCookieName]);
                if (testAcc != null) if (AuthTools.ValidateSiteAuthToken(testAcc.Name, testAcc.Password, Request[GlobalConst.ASmallCakeCookieName])) acc = testAcc;
            }
            if (acc == null) if (Request[GlobalConst.LoginCookieName] != null) acc = AuthServiceClient.VerifyAccountHashed(Request[GlobalConst.LoginCookieName], Request[GlobalConst.PasswordHashCookieName]);

            if (acc != null) {
                var ip = GetUserIP();
                using (var db = new ZkDataContext()) {
                    var penalty = Punishment.GetActivePunishment(acc.AccountID, ip, null, x => x.BanSite, db);
                    if (penalty != null) {
                        Response.Write(string.Format("You are banned! (IP match to account {0})\n", penalty.AccountByAccountID.Name));
                        Response.Write(string.Format("Ban expires: {0} UTC\n", penalty.BanExpires));
                        Response.Write(string.Format("Reason: {0}\n", penalty.Reason));
                        Response.End();
                    }
                    else {
                        HttpContext.Current.User = acc;
                        // todo replace with safer permanent cookie
                        Response.SetCookie(new HttpCookie(GlobalConst.LoginCookieName, acc.Name) { Expires = DateTime.Now.AddMonths(12) });
                        Response.SetCookie(new HttpCookie(GlobalConst.PasswordHashCookieName, acc.Password) { Expires = DateTime.Now.AddMonths(12) });
                    }
                }
            }
        }

        void OnPostAcquireRequestState(object sender, EventArgs eventArgs) {
            if (Request.QueryString["weblobby"] != null) {
                // save weblobby info
                Session["weblobby"] = Request.QueryString["weblobby"];
            }

            if (Request.QueryString["zkl"] != null) Session["zkl"] = Request.QueryString["zkl"];
        }
    }
}