using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace ZeroKWeb
{
	// Note: For instructions on enabling IIS6 or IIS7 classic mode, 
	// visit http://go.microsoft.com/?LinkId=9394801

	public class MvcApplication: HttpApplication
	{
		public MvcApplication()
		{
			AuthenticateRequest += MvcApplication_AuthenticateRequest;
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

		void MvcApplication_AuthenticateRequest(object sender, EventArgs e) {}
	}
}