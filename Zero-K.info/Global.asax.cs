﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using ZeroKWeb.Controllers;
using ZkData;

namespace ZeroKWeb
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : HttpApplication
    {
        private const string DbListKey = "ZkDataContextList";
        private DateTime lastPollCheck = DateTime.UtcNow;

        public MvcApplication()
        {
            ZkDataContext.DataContextCreated += context =>
            {
                if (HttpContext.Current != null)
                {
                    var dbs = HttpContext.Current.Items[DbListKey] as List<ZkDataContext>;
                    if (dbs != null) dbs.Add(context);
                }
            };
            BeginRequest += (sender, args) => { HttpContext.Current.Items[DbListKey] = new List<ZkDataContext>(); };
            EndRequest += (sender, args) =>
            {
                var dbs = HttpContext.Current.Items[DbListKey] as List<ZkDataContext>;
                if (dbs != null)
                    foreach (var db in dbs)
                    {
                        try
                        {
                            db.Dispose();
                        }
                        catch { }
                        ;
                    }
            };

            PostAuthenticateRequest += MvcApplication_PostAuthenticateRequest;
            PostAcquireRequestState += OnPostAcquireRequestState;
            Error += MvcApplication_Error;
        }

        public override string GetVaryByCustomString(HttpContext context, string custom)
        {
            if (custom == GlobalConst.LobbyAccessCookieName) return Global.IsLobbyAccess.ToString();
            return base.GetVaryByCustomString(context, custom);
        }


        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("Resources/{*pathInfo}");
            routes.IgnoreRoute("autoregistrator/maps/{*pathInfo}");
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


        protected void Application_End()
        {
            Global.StopApplication();
        }

        protected void Application_Start()
        {
            ViewEngines.Engines.Clear();
            ViewEngines.Engines.Add(new RazorViewEngine() { FileExtensions = new[] { "cshtml" } }); // this should speed up rendering a bit

            BundleConfig.RegisterBundles(BundleTable.Bundles);
            AreaRegistration.RegisterAllAreas();
            RegisterRoutes(RouteTable.Routes);

            Global.StartApplication(this);
        }

        private string GetUserIP()
        {
            return GetUserIP(new HttpRequestWrapper(Context.Request));
        }

        public static string GetUserIP(HttpRequestBase request)
        {
            string hostname = request.ServerVariables["REMOTE_HOST"];
            IPAddress[] ipv4s = Array.FindAll(Dns.GetHostEntry(hostname).AddressList,
                a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            if (ipv4s.Length > 0) return ipv4s[0].ToString();
            return request.ServerVariables["REMOTE_ADDR"];
        }

        private void MvcApplication_Error(object sender, EventArgs e)
        {
            var ex = Context.Server.GetLastError();
            if (!ex.Message.Contains("was not found or does not implement IController")) Trace.TraceError(ex.ToString());
            //var context = HttpContext.Current;
            //context.Server.ClearError();
        }


        private void MvcApplication_PostAuthenticateRequest(object sender, EventArgs e)
        {
            if (DateTime.UtcNow.Subtract(lastPollCheck).TotalMinutes > 15)
            {
                PollController.AutoClosePolls(); // this is silly here, should be a seaprate timer/thread
                lastPollCheck = DateTime.UtcNow;
            }

            Account acc = null;
            if (FormsAuthentication.IsEnabled && User.Identity.IsAuthenticated) acc = Account.AccountByName(new ZkDataContext(), User.Identity.Name);
            else if (Request[GlobalConst.SessionTokenVariable] != null)
            {
                int id = 0;
                if (Global.Server?.SessionTokens.TryRemove(Request[GlobalConst.SessionTokenVariable], out id) == true)
                {
                    acc = new ZkDataContext().Accounts.Find(id);
                }
            }

            if (acc == null) if (Request[GlobalConst.LoginCookieName] != null) acc = AuthServiceClient.VerifyAccountHashed(Request[GlobalConst.LoginCookieName], Request[GlobalConst.PasswordHashCookieName]);

            if (acc != null)
            {
                var ip = GetUserIP();
                var penalty = Punishment.GetActivePunishment(acc.AccountID, ip, null, x => x.BanSite);
                if (penalty != null)
                {
                    Response.Write(string.Format("You are banned! (IP match to account {0})\n", penalty.AccountByAccountID.Name));
                    Response.Write(string.Format("Ban expires: {0} UTC\n", penalty.BanExpires));
                    Response.Write(string.Format("Reason: {0}\n", penalty.Reason));
                    Response.End();
                }
                else
                {
                    HttpContext.Current.User = acc;
                    FormsAuthentication.SetAuthCookie(acc.Name, false);
                }
            }
        }

        private void OnPostAcquireRequestState(object sender, EventArgs eventArgs)
        {
            if (Request.QueryString["weblobby"] != null) Session["weblobby"] = Request.QueryString["weblobby"];

            if (Request.QueryString["zkl"] != null) Session["zkl"] = Request.QueryString["zkl"];
        }
    }
}