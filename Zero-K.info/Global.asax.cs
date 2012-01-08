using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using CaTracker;
using ZkData;

namespace ZeroKWeb
{
	// Note: For instructions on enabling IIS6 or IIS7 classic mode, 
	// visit http://go.microsoft.com/?LinkId=9394801

	public class MvcApplication: HttpApplication
	{
		public MvcApplication()
		{
			PostAuthenticateRequest += MvcApplication_PostAuthenticateRequest;
			this.Error += MvcApplication_Error;
        }

     
		void MvcApplication_Error(object sender, EventArgs e)
		{
			if (Request.Url.ToString().Contains(".mvc")) {
				Response.RedirectPermanent(Request.Url.ToString().Replace(".mvc", ""));
			} else Server.GetLastError();
		}


		public static void RegisterRoutes(RouteCollection routes)
		{
			routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

			routes.MapRoute("WikiPage", "Wiki/{node}", new { controller = "Wiki", action = "Index", node = UrlParameter.Optional });
			routes.MapRoute("WikiPage2", "p/zero-k/wiki/{node}", new { controller = "Wiki", action = "Index", node = UrlParameter.Optional });
			routes.MapRoute("MissionImage", "Missions/Img/{name}", new { controller = "Missions", action = "Img", name = UrlParameter.Optional });
			routes.MapRoute("MissionFile", "Missions/File/{name}", new { controller = "Missions", action = "File", name = UrlParameter.Optional });
			routes.MapRoute("StaticFile", "Static/{name}", new { controller = "Static", action = "Index", name = UrlParameter.Optional });
		
			routes.MapRoute("Default", "{controller}/{action}/{id}", new { controller="Home", action = "Index", id = UrlParameter.Optional });

			routes.MapRoute("Root", "", new { controller = "Home", action = "Index", id = "" });

			

		}

		public override string GetVaryByCustomString(HttpContext context, string custom)
		{
			if (custom == GlobalConst.LobbyAccessCookieName) return Global.IsLobbyAccess.ToString();
			return base.GetVaryByCustomString(context, custom);
		}

		protected void Application_Start()
		{
		    /*List<string> links;
		    byte[] tor;
		    List<string> dep;
		    ResourceType rt;
		    string fn;
            ResourceLinkProvider.GetLinksAndTorrent("Zero-K v0.8.14", out links, out tor, out dep, out rt, out fn);
            */
            Application["Nightwatch"] = new Nightwatch(Server.MapPath("/"));
            Global.Nightwatch.Start();

			AreaRegistration.RegisterAllAreas();
			RegisterRoutes(RouteTable.Routes);

		    
		}

		void MvcApplication_PostAuthenticateRequest(object sender, EventArgs e)
		{
			
			if (Request[GlobalConst.LoginCookieName] != null)
			{
				var acc = AuthServiceClient.VerifyAccountHashed(Request[GlobalConst.LoginCookieName], Request[GlobalConst.PasswordHashCookieName]);
                if (acc != null)
                {
                    if (acc.Punishments.Any(x=>x.BanExpires > DateTime.UtcNow && x.BanSite)) {
                        Response.Write("Banned!");
                        Response.End();
                    } else HttpContext.Current.User = acc;
                }
			}
		}
	}
}