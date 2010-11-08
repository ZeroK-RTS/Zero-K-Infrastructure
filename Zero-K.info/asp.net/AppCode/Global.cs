using System.Web;
using ZkData;

namespace ZeroKWeb
{
	public static class Global
	{
		public static Account Account { get { return HttpContext.Current.User as Account; } }
		public static bool IsAccountAuthorized { get { return HttpContext.Current.User as Account != null; } }
		public static bool IsLobbyAccess { get { return HttpContext.Current.Request.Cookies[GlobalConst.LobbyAccessCookieName] != null; } }
	}
}