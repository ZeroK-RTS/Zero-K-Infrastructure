using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using ServiceData;

namespace PlanetWars.Web
{
	/// <summary>
	/// Summary description for SpringAuthService
	/// </summary>
	[WebService(Namespace = "http://planet-wars.eu/SpringAuthService")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[System.ComponentModel.ToolboxItem(false)]
	// To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
	// [System.Web.Script.Services.ScriptService]
	public class SpringAuthService : System.Web.Services.WebService
	{

		[WebMethod]
		public bool VerifySpringAccount(string login, string password)
		{
			var db = new DbDataContext();
			return db.SpringAccounts.Any(x => x.Name == login && x.Password == SpringAccount.HashPassword(password));
		}
	}
}
