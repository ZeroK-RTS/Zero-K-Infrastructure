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
    public static bool ShowFeaturedByDefault
    {
      get
      {
        return Account == null || Account.LobbyTimeRank <= 3;
      }
    }

    public static bool IsAccountAuthorized { get { return HttpContext.Current.User as Account != null; } }
    public static bool IsAdmin
    {
      get { return Account != null && Account.IsAdmin; }
    }
      public static bool IsLimitedMode
    {
      get
      {
        if (HttpContext.Current.Request[GlobalConst.LimitedModeCookieName] == "0") return false;
        var cook = HttpContext.Current.Request.Cookies[GlobalConst.LimitedModeCookieName];
        if (cook != null && cook.Value == "0") return false;
        return true;
      }
    }
    public static bool IsLobbyAccess { get { return HttpContext.Current.Request.Cookies[GlobalConst.LobbyAccessCookieName] != null; } }
  }
}