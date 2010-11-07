using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using ZkData;

namespace ZeroKWeb
{
	// Note: For instructions on enabling IIS6 or IIS7 classic mode, 
	// visit http://go.microsoft.com/?LinkId=9394801

	public class MvcApplication: HttpApplication
	{
		public MvcApplication()
		{
			PostAuthenticateRequest += new EventHandler(MvcApplication_PostAuthenticateRequest);
		}


		void MvcApplication_PostAuthenticateRequest(object sender, EventArgs e)
		{
			if (Request[GlobalConst.LoginCookieName] != null)
			{
				var acc = AuthServiceClient.VerifyAccountHashed(Request[GlobalConst.LoginCookieName], Request[GlobalConst.PasswordHashCookieName]);
				if (acc != null) HttpContext.Current.User = acc;
			}
			if (Debugger.IsAttached)
			{
				//var db = new ZkDataContext();
				//HttpContext.Current.User = db.Accounts.First(x => x.Name == "[0K]Licho");
			} 
		}


		public static void RegisterRoutes(RouteCollection routes)
		{
			routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

			routes.MapRoute("MissionImage", "Missions.mvc/Img/{name}", new { controller = "Missions", action = "Img", name = UrlParameter.Optional });
			routes.MapRoute("MissionFile", "Missions.mvc/File/{name}", new { controller = "Missions", action = "File", name = UrlParameter.Optional });

			routes.MapRoute("Default", "{controller}.mvc/{action}/{id}", new { controller = "Home", action = "Index", id = UrlParameter.Optional });

			//routes.MapRoute("Root", "", new { controller = "Home", action = "Index", id = "" });
		}

		protected void Application_Start()
		{
			AreaRegistration.RegisterAllAreas();

			RegisterRoutes(RouteTable.Routes);
		}

		
	}
}