using System.Web;
using ZkData;

namespace ZeroKWeb
{
  public static class Global
  {
    public const int AjaxScrollCount = 40;
    public static Account Account { get { return HttpContext.Current.User as Account; } }
    public static int AccountID
    {
      get
      {
        if (IsAccountAuthorized) return Account.AccountID;
        else return 0;
      }
    }
    public static bool IsAccountAuthorized { get { return HttpContext.Current.User as Account != null; } }
    public static bool IsLimitedMode
    {
      get
      {
        return HttpContext.Current.Request[GlobalConst.LimitedModeCookieName] != "0";
      }
    }
    public static bool IsLobbyAccess { get { return HttpContext.Current.Request.Cookies[GlobalConst.LobbyAccessCookieName] != null && IsAccountAuthorized; } }
  }
}