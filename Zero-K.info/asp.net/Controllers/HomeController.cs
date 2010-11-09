using System;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using PlasmaShared;
using ZkData;

namespace ZeroKWeb.Controllers
{
	public class HomeController: Controller
	{
		//
		// GET: /Home/


		public static string GetMissionTooltip(int id)
		{
			var db = new ZkDataContext();
			var sb = new StringBuilder();
			var mis = db.Missions.Single(x => x.MissionID == id);

			sb.AppendFormat("{0}<br/>---<br/>", HttpUtility.HtmlDecode(mis.Description));
			sb.AppendFormat("Players: {0} - {1}<br/>", mis.MinHumans, mis.MaxHumans);
			sb.AppendFormat("Comments: {0}<br/>", mis.ForumThread != null ? mis.ForumThread.ForumPosts.Count : 0);
			sb.AppendFormat("Rated by: {0} players", mis.Ratings.Count);
			return sb.ToString();
		}

		public ActionResult GetTooltip(string key)
		{
			var args = key.Split(new char[] { '$' }, StringSplitOptions.RemoveEmptyEntries);
			var ret = "";
			switch (args[0])
			{
				case "mission":

					ret = GetMissionTooltip(int.Parse(args[1]));
					break;
			}
			return Content(ret);
		}


		public ActionResult Logon(string login, string password, string referer)
		{
			var db = new ZkDataContext();

			var acc = db.Accounts.SingleOrDefault(x => x.Name == login);
			if (acc == null) return Content("Invalid login name");
			var hashed = Utils.HashLobbyPassword(password);
			acc = AuthServiceClient.VerifyAccountHashed(login, hashed);
			if (acc == null) return Content("Invalid password");
			else
			{
				Response.SetCookie(new HttpCookie(GlobalConst.LoginCookieName, login));
				Response.SetCookie(new HttpCookie(GlobalConst.PasswordHashCookieName, hashed));

				return Redirect(referer);
			}
		}
	}
}