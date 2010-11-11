using System;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using PlasmaShared;
using PlasmaShared.UnitSyncLib;
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

			sb.AppendFormat("{0}<br/>---<br/>", HttpUtility.HtmlEncode(mis.Description??"").Replace("\n","<br/>"));
			sb.AppendFormat("Players: {0}<br/>", mis.MinToMaxHumansString);
			sb.AppendFormat("<small>{0}</small><br/>", string.Join(",", mis.GetPseudoTags()));
			sb.AppendFormat("Map: {0}<br/>", mis.Map);
			sb.AppendFormat("Game: {0}<br/>", mis.Mod ?? mis.ModRapidTag);
			sb.AppendFormat("Played: {0} times<br/>", mis.MissionRunCount);
			sb.AppendFormat("Rated: {0} times<br/>", mis.Ratings.Count);
			sb.AppendFormat("Comments: {0}<br/>", mis.ForumThread != null ? mis.ForumThread.ForumPosts.Count : 0);

			return sb.ToString();
		}

    [NoCache]
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